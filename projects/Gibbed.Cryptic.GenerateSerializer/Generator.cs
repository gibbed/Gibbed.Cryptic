/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Gibbed.Cryptic.FileFormats;
using Parser = Gibbed.Cryptic.FileFormats.Parser;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;
using Serialization = Gibbed.Cryptic.FileFormats.Serialization;

namespace Gibbed.Cryptic.GenerateSerializer
{
    internal class Generator
    {
        private AssemblyBuilder _AssemblyBuilder;
        private ModuleBuilder _ModuleBuilder;

        private readonly TargetGame _TargetGame;
        private readonly string _BaseTypeName;

        private readonly ParserLoader _ParserLoader;
        private readonly EnumLoader _EnumLoader;

        public Generator(TargetGame targetGame, ParserLoader parserLoader, EnumLoader enumLoader)
        {
            this._TargetGame = targetGame;
            this._BaseTypeName = "Gibbed." + this._TargetGame + ".Serialization.";
            this._ParserLoader = parserLoader;
            this._EnumLoader = enumLoader;
        }

        private static MethodInfo _EnumTryParse =
            typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == "TryParse" && m.GetParameters().Length == 3);

        private static ConstructorInfo GetConstructor(Type type, params Type[] types)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var info = type.GetConstructor(types);
            if (info == null)
            {
                throw new InvalidOperationException(string.Format("missing constructor for {0}", type.Name));
            }
            return info;
        }

        private static ConstructorInfo GetConstructor<T>(params Type[] types)
        {
            return GetConstructor(typeof(T), types);
        }

        public void ExportAssembly(string outputPath, string version)
        {
            var fileName = Path.GetFileName(outputPath);
            if (string.IsNullOrEmpty(fileName) == true)
            {
                throw new InvalidOperationException();
            }

            var assemblyName = new AssemblyName
            {
                Name = Path.GetFileNameWithoutExtension(outputPath),
            };

            this._AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Save);

            if (version != null)
            {
                this._AssemblyBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<AssemblyDescriptionAttribute>(typeof(string)),
                        new object[] { version }));
            }

            this._AssemblyBuilder.DefineVersionInfoResource();

            this._ModuleBuilder = this._AssemblyBuilder.DefineDynamicModule(
                assemblyName.Name,
                fileName);

            this._EnumTypes = new Dictionary<ParserSchema.Enumeration, Type>();
            this._TableTypes = new Dictionary<ParserSchema.Table, TypeBuilder>();

            var queuedTypes = new Queue<QueuedType>();
            var done = new List<string>();

            queuedTypes.Clear();
            done.Clear();

            Console.WriteLine("Loading parsers...");
            foreach (var name in this._ParserLoader.ParserNames)
            {
                queuedTypes.Enqueue(new QueuedType(name, this._ParserLoader.LoadParser(name).Table));
            }

            while (queuedTypes.Count > 0)
            {
                var queuedType = queuedTypes.Dequeue();
                done.Add(queuedType.Key);

                Console.WriteLine("Generating type for {0}...", queuedType.Key);

                TypeBuilder typeBuilder;

                if (queuedType.Parent == null)
                {
                    typeBuilder = this._ModuleBuilder.DefineType(
                        this._BaseTypeName + queuedType.Name,
                        TypeAttributes.Public,
                        null,
                        new[] { typeof(Serialization.IStructure) });
                }
                else
                {
                    typeBuilder = this._TableTypes[queuedType.Parent.Table].DefineNestedType(
                        queuedType.Name,
                        TypeAttributes.NestedPublic,
                        null,
                        new[] { typeof(Serialization.IStructure) });
                }

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<DataContractAttribute>(),
                        new object[] { },
                        new[] { typeof(DataContractAttribute).GetProperty("Namespace") },
                        new object[]
                        {
                            "http://datacontract.gib.me/" +
                            this._TargetGame.ToString().ToLowerInvariant()
                        }));

                this._TableTypes.Add(queuedType.Table, typeBuilder);

                foreach (var column in queuedType.Table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if (column.Token == Parser.TokenType.Structure)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queuedTypes.Any(e => e.Name == column.SubtableExternalName && e.Parent == null) == false)
                            {
                                queuedTypes.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = queuedType.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queuedTypes.Any(e => e.Name == column.Name && e.Parent == queuedType) == false)
                            {
                                queuedTypes.Enqueue(new QueuedType(column.Name, column.Subtable, queuedType));
                            }
                        }
                    }
                    else if (column.Token == Parser.TokenType.Polymorph)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.SubtableIsExternal == true)
                            {
                                if (done.Contains(subcolumn.SubtableExternalName) == false &&
                                    queuedTypes.Any(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null) ==
                                    false)
                                {
                                    queuedTypes.Enqueue(new QueuedType(subcolumn.SubtableExternalName,
                                                                       subcolumn.Subtable));
                                }
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }
                }
            }

            queuedTypes.Clear();
            done.Clear();

            foreach (var name in this._ParserLoader.ParserNames)
            {
                queuedTypes.Enqueue(new QueuedType(name, this._ParserLoader.LoadParser(name).Table));
            }

            while (queuedTypes.Count > 0)
            {
                var qt = queuedTypes.Dequeue();
                done.Add(qt.Key);

                Console.WriteLine("Generating code for {0}...", qt.Key);

                var typeBuilder = this._TableTypes[qt.Table];

                this.ExportStructure(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if (column.Token == Parser.TokenType.Structure)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queuedTypes.Any(e => e.Name == column.SubtableExternalName && e.Parent == null) == false)
                            {
                                queuedTypes.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = qt.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queuedTypes.Any(e => e.Name == column.Name && e.Parent == qt) == false)
                            {
                                queuedTypes.Enqueue(new QueuedType(column.Name, column.Subtable, qt));
                            }
                        }
                    }
                    else if (column.Token == Parser.TokenType.Polymorph)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.SubtableIsExternal == true)
                            {
                                if (done.Contains(subcolumn.SubtableExternalName) == false &&
                                    queuedTypes.Count(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null) ==
                                    0)
                                {
                                    queuedTypes.Enqueue(new QueuedType(subcolumn.SubtableExternalName,
                                                                       subcolumn.Subtable));
                                }
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }
                    }
                }

                typeBuilder.CreateType();
            }

            this._AssemblyBuilder.Save(outputPath);
        }

        private Dictionary<ParserSchema.Table, TypeBuilder> _TableTypes;
        private Dictionary<ParserSchema.Enumeration, Type> _EnumTypes;

        private Type GetColumnNativeType(TypeBuilder parent,
                                         ParserSchema.Column column)
        {
            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                Type e;

                if (column.StaticDefineListIsExternal == true)
                {
                    e = this.GenerateEnum(parent,
                                          column,
                                          this._EnumLoader.LoadEnum(column.StaticDefineListExternalName).Enum);
                }
                else
                {
                    e = this.GenerateEnum(parent, column, column.StaticDefineList);
                }

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
                {
                    return e;
                }

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                {
                    return e.MakeArrayType();
                }

                return typeof(List<>).MakeGenericType(e);
            }

            if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
            {
                switch (column.Token)
                {
                    case Parser.TokenType.Byte:
                        return typeof(byte);
                    case Parser.TokenType.Int16:
                        return typeof(short);
                    case Parser.TokenType.Int32:
                        return typeof(int);
                    case Parser.TokenType.Int64:
                        return typeof(long);
                    case Parser.TokenType.Float:
                        return typeof(float);
                    case Parser.TokenType.String:
                        return typeof(string);
                    case Parser.TokenType.CurrentFile:
                        return typeof(string);
                    case Parser.TokenType.Timestamp:
                        return typeof(int);
                    case Parser.TokenType.LineNumber:
                        return typeof(int);
                    case Parser.TokenType.Boolean:
                        return typeof(bool);
                    case Parser.TokenType.BooleanFlag:
                        return typeof(bool);
                    case Parser.TokenType.MATPYR:
                        return typeof(MATPYR);
                    case Parser.TokenType.Filename:
                        return typeof(string);
                    case Parser.TokenType.Reference:
                        return typeof(string);
                    case Parser.TokenType.Structure:
                        return this._TableTypes[column.Subtable];
                    case Parser.TokenType.Polymorph:
                        return typeof(object);
                    case Parser.TokenType.StashTable:
                        return typeof(StashTable);
                    case Parser.TokenType.Bit:
                        return typeof(uint);
                    case Parser.TokenType.MultiValue:
                        return typeof(MultiValue);
                    case Parser.TokenType.Command:
                        return typeof(string);
                    default:
                        throw new NotImplementedException();
                }
            }
            else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
            {
                switch (column.Token)
                {
                    case Parser.TokenType.Byte:
                        return typeof(byte[]);
                    case Parser.TokenType.Int16:
                        return typeof(short[]);
                    case Parser.TokenType.Int32:
                        return typeof(int[]);
                    case Parser.TokenType.Int64:
                        return typeof(long[]);
                    case Parser.TokenType.Float:
                        return typeof(float[]);
                    case Parser.TokenType.String:
                        return typeof(string[]);
                    case Parser.TokenType.CurrentFile:
                        return typeof(string[]);
                    case Parser.TokenType.QUATPYR:
                        return typeof(float[]);
                    case Parser.TokenType.MATPYR:
                        return typeof(MATPYR[]);
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (column.Token)
                {
                    case Parser.TokenType.Byte:
                        return typeof(List<byte>);
                    case Parser.TokenType.Int16:
                        return typeof(List<short>);
                    case Parser.TokenType.Int32:
                        return typeof(List<int>);
                    case Parser.TokenType.Int64:
                        return typeof(List<long>);
                    case Parser.TokenType.Float:
                        return typeof(List<float>);
                    case Parser.TokenType.String:
                        return typeof(List<string>);
                    case Parser.TokenType.CurrentFile:
                        return typeof(List<string>);
                    case Parser.TokenType.NoAST:
                        return typeof(List<object>);
                    case Parser.TokenType.MATPYR:
                        return typeof(List<MATPYR>);
                    case Parser.TokenType.Filename:
                        return typeof(List<string>);
                    case Parser.TokenType.FunctionCall:
                        return typeof(List<FunctionCall>);
                    case Parser.TokenType.Structure:
                        return typeof(List<>).MakeGenericType(this._TableTypes[column.Subtable]);
                    case Parser.TokenType.Polymorph:
                        return typeof(List<object>);
                    case Parser.TokenType.MultiValue:
                        return typeof(List<MultiValue>);
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private object ChangeEnumType(int value, Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                {
                    return (byte)value;
                }

                case TypeCode.Int16:
                {
                    return (short)value;
                }

                case TypeCode.Int32:
                {
                    return value;
                }

                case TypeCode.UInt32:
                {
                    return (uint)value;
                }
            }

            throw new NotSupportedException();
        }

        private Type GenerateEnum(
            TypeBuilder parent,
            ParserSchema.Column column,
            ParserSchema.Enumeration e)
        {
            if (this._EnumTypes.ContainsKey(e) == true)
            {
                return this._EnumTypes[e];
            }

            if (e.Type != ParserSchema.EnumerationType.Int)
            {
                throw new NotSupportedException();
            }

            Type underlyingType;

            if (column.Format == ParserSchema.ColumnFormat.None ||
                column.Format == ParserSchema.ColumnFormat.Flags)
            {
                switch (column.Token)
                {
                    case Parser.TokenType.Byte:
                        underlyingType = typeof(byte);
                        break;
                    case Parser.TokenType.Int16:
                        underlyingType = typeof(short);
                        break;
                    case Parser.TokenType.Int32:
                        underlyingType = typeof(int);
                        break;
                    case Parser.TokenType.Bit:
                        underlyingType = typeof(uint);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else if (column.Format == ParserSchema.ColumnFormat.Color)
            {
                switch (column.Token)
                {
                    case Parser.TokenType.Int32:
                        underlyingType = typeof(uint);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            if (column.StaticDefineListIsExternal == true)
            {
                //var name = column.StaticDefineListExternalName;

                var builder = this._ModuleBuilder.DefineEnum(
                    this._BaseTypeName + "StaticDefineList." + column.StaticDefineListExternalName,
                    TypeAttributes.Public,
                    underlyingType);

                if (column.Format == ParserSchema.ColumnFormat.Flags ||
                    column.Name.ToLowerInvariant().Contains("flag") == true)
                {
                    builder.SetCustomAttribute(
                        new CustomAttributeBuilder(GetConstructor<FlagsAttribute>(), new object[0]));
                }

                foreach (var kv in e.Elements)
                {
                    var value = ChangeEnumType(int.Parse(kv.Value), underlyingType);
                    builder.DefineLiteral(kv.Key, value);
                }

                var type = builder.CreateType();
                this._EnumTypes.Add(e, type);
                return type;
            }
            else
            {
                var builder = parent.DefineNestedType(
                    column.Name + "_DefineList",
                    TypeAttributes.NestedPublic | TypeAttributes.Sealed,
                    typeof(Enum),
                    null);

                if (column.Format == ParserSchema.ColumnFormat.Flags ||
                    column.Name.ToLowerInvariant().Contains("flag") == true)
                {
                    builder.SetCustomAttribute(
                        new CustomAttributeBuilder(GetConstructor<FlagsAttribute>(), new object[0]));
                }

                builder.DefineField(
                    "value__",
                    typeof(int),
                    FieldAttributes.Private | FieldAttributes.SpecialName);

                foreach (var kv in e.Elements)
                {
                    var value = ChangeEnumType(int.Parse(kv.Value), underlyingType);
                    var fieldBuilder = builder.DefineField(
                        kv.Key,
                        builder,
                        FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                    fieldBuilder.SetConstant(value);
                }

                var type = builder.CreateType();
                this._EnumTypes.Add(e, type);
                return type;
            }
        }

        private Type ExportFieldEnum(
            ParserSchema.Table table,
            TypeBuilder structure)
        {
            var builder = this._ModuleBuilder.DefineEnum(
                this._BaseTypeName + "Fields." + structure.Name + "Field",
                TypeAttributes.Public,
                typeof(int));

            int index = 0;
            foreach (var column in table.Columns)
            {
                if (table.Columns.Count(c => c.Name == column.Name) > 1)
                {
                    builder.DefineLiteral(column.Name + "_" + index.ToString(CultureInfo.InvariantCulture), index);
                }
                else
                {
                    builder.DefineLiteral(column.Name, index);
                }

                index++;
            }
            return builder.CreateType();
        }

        private static object ConvertDefaultValue(int defaultValue, TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.SByte:
                {
                    return (sbyte)defaultValue;
                }
                case TypeCode.Byte:
                {
                    return (byte)defaultValue;
                }

                case TypeCode.Int16:
                {
                    return (short)defaultValue;
                }

                case TypeCode.UInt16:
                {
                    return (ushort)defaultValue;
                }

                case TypeCode.Int32:
                {
                    return defaultValue;
                }

                case TypeCode.UInt32:
                {
                    return (uint)defaultValue;
                }

                case TypeCode.Int64:
                {
                    return (long)defaultValue;
                }

                case TypeCode.UInt64:
                {
                    return (ulong)defaultValue;
                }
            }

            return Convert.ChangeType(defaultValue, typeCode);
        }

        private FieldBuilder ExportField(
            ParserSchema.Table table,
            ParserSchema.Column column,
            TypeBuilder structure,
            out bool isSpecialDefault)
        {
            isSpecialDefault = false;

            var token = Parser.GlobalTokens.GetToken(column.Token);

            var name = Helpers.GetColumnName(table, column);
            var type = GetColumnNativeType(structure, column);

            object defaultValue = null;

            switch (token.GetParameter(column.Flags, 0))
            {
                case Parser.ColumnParameter.Default:
                {
                    if (column.Default != 0)
                    {
                        defaultValue = ConvertDefaultValue(column.Default, Type.GetTypeCode(type));
                    }

                    break;
                }

                case Parser.ColumnParameter.DefaultString:
                {
                    if (column.DefaultString != null)
                    {
                        defaultValue = column.DefaultString;
                    }

                    break;
                }
            }

            if (defaultValue == null)
            {
                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
                {
                }
                else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                {
                }
                else
                {
                    isSpecialDefault = true;
                }
            }

            var attributes = FieldAttributes.Public;
            if (defaultValue != null)
            {
                attributes |= FieldAttributes.HasDefault;
            }

            var builder = structure.DefineField(name, type, attributes);

            if (defaultValue != null)
            {
                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    builder.SetConstant(Enum.ToObject(type, defaultValue));
                }
                else
                {
                    builder.SetConstant(defaultValue);
                }
            }

            if (type == typeof(int[]) &&
                column.Name == "bf")
            {
                var fieldEnum = ExportFieldEnum(table, structure);
                var wrapperType = typeof(FieldList<>).MakeGenericType(fieldEnum);

                var wrapperName = "__wrapper_" + name;

                var wrapperBuilder = structure.DefineProperty(
                    wrapperName,
                    PropertyAttributes.None,
                    wrapperType,
                    Type.EmptyTypes);

                var getBuilder = structure.DefineMethod(
                    "get_" + wrapperName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    wrapperType,
                    Type.EmptyTypes);

                var getIL = getBuilder.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, builder);
                getIL.EmitConstant(column.NumberOfElements);
                getIL.Emit(OpCodes.Call, typeof(FieldListParser<>).MakeGenericType(fieldEnum).GetMethod("ToList"));
                getIL.Emit(OpCodes.Ret);

                var setBuilder = structure.DefineMethod(
                    "set_" + wrapperName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null,
                    new[] { wrapperType });

                var setIL = setBuilder.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.EmitConstant(column.NumberOfElements);
                setIL.Emit(OpCodes.Call, typeof(FieldListParser<>).MakeGenericType(fieldEnum).GetMethod("FromList"));
                setIL.Emit(OpCodes.Stfld, builder);
                setIL.Emit(OpCodes.Ret);

                wrapperBuilder.SetGetMethod(getBuilder);
                wrapperBuilder.SetSetMethod(setBuilder);

                wrapperBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<DataMemberAttribute>(),
                        new object[0],
                        new[]
                        {
                            typeof(DataMemberAttribute).GetProperty("Name"),
                            typeof(DataMemberAttribute).GetProperty("Order"),
                            typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                        },
                        new object[]
                        {
                            name,
                            table.Columns.IndexOf(column),
                            false,
                        }));

                wrapperBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<CollectionDataContractAttribute>(),
                        new object[0],
                        new[]
                        {
                            typeof(CollectionDataContractAttribute).GetProperty("Name"),
                            typeof(CollectionDataContractAttribute).GetProperty("ItemName"),
                        },
                        new object[]
                        {
                            structure.Name + "SetField",
                            "Field",
                        }));
            }
            else if (type == typeof(List<MultiValue>) &&
                     string.IsNullOrEmpty(column.Name) == true)
            {
                var wrapperName = "__wrapper_" + name;

                var wrapperBuilder = structure.DefineProperty(
                    wrapperName,
                    PropertyAttributes.None,
                    typeof(CDataWrapper),
                    Type.EmptyTypes);

                var getBuilder = structure.DefineMethod(
                    "get_" + wrapperName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    typeof(CDataWrapper),
                    Type.EmptyTypes);

                var getIL = getBuilder.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, builder);
                getIL.Emit(OpCodes.Call, typeof(PCodeParser).GetMethod("ToStringValue"));
                getIL.Emit(OpCodes.Ret);

                var setBuilder = structure.DefineMethod(
                    "set_" + wrapperName,
                    MethodAttributes.Public |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null,
                    new[] { typeof(CDataWrapper) });

                var setIL = setBuilder.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Call, typeof(PCodeParser).GetMethod("FromStringValue"));
                setIL.Emit(OpCodes.Stfld, builder);
                setIL.Emit(OpCodes.Ret);

                wrapperBuilder.SetGetMethod(getBuilder);
                wrapperBuilder.SetSetMethod(setBuilder);

                wrapperBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<DataMemberAttribute>(),
                        new object[0],
                        new[]
                        {
                            typeof(DataMemberAttribute).GetProperty("Name"),
                            typeof(DataMemberAttribute).GetProperty("Order"),
                            typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                        },
                        new object[]
                        {
                            name,
                            table.Columns.IndexOf(column),
                            false,
                        }));
            }
            else if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                     column.StaticDefineList != null)
            {
                var wrapperName = "__wrapper_" + name;

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY) == false)
                {
                    var wrapperBuilder = structure.DefineProperty(
                        wrapperName,
                        PropertyAttributes.None,
                        typeof(string),
                        Type.EmptyTypes);

                    var getBuilder = structure.DefineMethod(
                        "get_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        typeof(string),
                        Type.EmptyTypes);

                    var getIL = getBuilder.GetILGenerator();
                    getIL.Emit(OpCodes.Ldarg_0);
                    getIL.Emit(OpCodes.Ldfld, builder);
                    getIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(type).GetMethod("ToStringValue"));
                    getIL.Emit(OpCodes.Ret);

                    var setBuilder = structure.DefineMethod(
                        "set_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        null,
                        new[] { typeof(string) });

                    var setIL = setBuilder.GetILGenerator();
                    setIL.Emit(OpCodes.Ldarg_0);
                    setIL.Emit(OpCodes.Ldarg_1);
                    setIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(type).GetMethod("FromStringValue"));
                    setIL.Emit(OpCodes.Stfld, builder);
                    setIL.Emit(OpCodes.Ret);

                    wrapperBuilder.SetGetMethod(getBuilder);
                    wrapperBuilder.SetSetMethod(setBuilder);

                    wrapperBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            GetConstructor<DefaultValueAttribute>(typeof(string)),
                            new object[] { Enum.GetName(type, 0) }));

                    wrapperBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            GetConstructor<DataMemberAttribute>(),
                            new object[0],
                            new[]
                            {
                                typeof(DataMemberAttribute).GetProperty("Name"),
                                typeof(DataMemberAttribute).GetProperty("Order"),
                                typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                            },
                            new object[]
                            {
                                name,
                                table.Columns.IndexOf(column),
                                false,
                            }));
                }
                else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                {
                    var elementType = type.GetElementType();

                    var wrapperBuilder = structure.DefineProperty(
                        wrapperName,
                        PropertyAttributes.None,
                        typeof(string[]),
                        Type.EmptyTypes);

                    var getBuilder = structure.DefineMethod(
                        "get_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        typeof(string[]),
                        Type.EmptyTypes);

                    var getIL = getBuilder.GetILGenerator();
                    getIL.Emit(OpCodes.Ldarg_0);
                    getIL.Emit(OpCodes.Ldfld, builder);
                    getIL.Emit(OpCodes.Call,
                               typeof(EnumParser<>).MakeGenericType(elementType).GetMethod("ToStringArray"));
                    getIL.Emit(OpCodes.Ret);

                    var setBuilder = structure.DefineMethod(
                        "set_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        null,
                        new[] { typeof(string[]) });

                    var setIL = setBuilder.GetILGenerator();
                    setIL.Emit(OpCodes.Ldarg_0);
                    setIL.Emit(OpCodes.Ldarg_1);
                    setIL.Emit(OpCodes.Call,
                               typeof(EnumParser<>).MakeGenericType(elementType).GetMethod("FromStringArray"));
                    setIL.Emit(OpCodes.Stfld, builder);
                    setIL.Emit(OpCodes.Ret);

                    wrapperBuilder.SetGetMethod(getBuilder);
                    wrapperBuilder.SetSetMethod(setBuilder);

                    wrapperBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            GetConstructor<DataMemberAttribute>(),
                            new object[0],
                            new[]
                            {
                                typeof(DataMemberAttribute).GetProperty("Name"),
                                typeof(DataMemberAttribute).GetProperty("Order"),
                                typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                            },
                            new object[]
                            {
                                name,
                                table.Columns.IndexOf(column),
                                false,
                            }));
                }
                else
                {
                    var elementType = type.GetGenericArguments()[0];

                    var wrapperBuilder = structure.DefineProperty(
                        wrapperName,
                        PropertyAttributes.None,
                        typeof(List<string>),
                        Type.EmptyTypes);

                    var getBuilder = structure.DefineMethod(
                        "get_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        typeof(List<string>),
                        Type.EmptyTypes);

                    var getIL = getBuilder.GetILGenerator();
                    getIL.Emit(OpCodes.Ldarg_0);
                    getIL.Emit(OpCodes.Ldfld, builder);
                    getIL.Emit(OpCodes.Call,
                               typeof(EnumParser<>).MakeGenericType(elementType).GetMethod("ToStringList"));
                    getIL.Emit(OpCodes.Ret);

                    var setBuilder = structure.DefineMethod(
                        "set_" + wrapperName,
                        MethodAttributes.Public |
                        MethodAttributes.SpecialName |
                        MethodAttributes.HideBySig,
                        null,
                        new[] { typeof(List<string>) });

                    var setIL = setBuilder.GetILGenerator();
                    setIL.Emit(OpCodes.Ldarg_0);
                    setIL.Emit(OpCodes.Ldarg_1);
                    setIL.Emit(OpCodes.Call,
                               typeof(EnumParser<>).MakeGenericType(elementType).GetMethod("FromStringList"));
                    setIL.Emit(OpCodes.Stfld, builder);
                    setIL.Emit(OpCodes.Ret);

                    wrapperBuilder.SetGetMethod(getBuilder);
                    wrapperBuilder.SetSetMethod(setBuilder);

                    wrapperBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            GetConstructor<DataMemberAttribute>(),
                            new object[0],
                            new[]
                            {
                                typeof(DataMemberAttribute).GetProperty("Name"),
                                typeof(DataMemberAttribute).GetProperty("Order"),
                                typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                            },
                            new object[]
                            {
                                name,
                                table.Columns.IndexOf(column),
                                false,
                            }));
                }
            }
            else
            {
                builder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<DataMemberAttribute>(),
                        new object[0],
                        new[]
                        {
                            typeof(DataMemberAttribute).GetProperty("Order"),
                            typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                        },
                        new object[]
                        {
                            table.Columns.IndexOf(column),
                            false,
                        }));
            }

            return builder;
        }

        private void ExportStructureSpecialDefaults(
            TypeBuilder typeBuilder,
            List<FieldBuilder> specialDefaultFieldBuilders)
        {
            var clearListsBuilder = typeBuilder.DefineMethod(
                "ClearLists",
                MethodAttributes.Private,
                null,
                Type.EmptyTypes);
            {
                var msil = clearListsBuilder.GetILGenerator();
                foreach (var fieldBuilder in specialDefaultFieldBuilders)
                {
                    var listType = fieldBuilder.FieldType;
                    if (listType.GetGenericTypeDefinition() != typeof(List<>))
                    {
                        throw new InvalidOperationException();
                    }

                    var label = msil.DefineLabel();

                    msil.Emit(OpCodes.Ldarg_0); // this
                    msil.Emit(OpCodes.Ldfld, fieldBuilder);
                    msil.Emit(OpCodes.Ldnull);
                    msil.Emit(OpCodes.Ceq);
                    msil.Emit(OpCodes.Brtrue, label);

                    msil.Emit(OpCodes.Ldarg_0); // this
                    msil.Emit(OpCodes.Ldfld, fieldBuilder);

                    var elementType = listType.GetGenericArguments().Single();
                    if (elementType is TypeBuilder)
                    {
                        msil.Emit(OpCodes.Callvirt,
                                  TypeBuilder.GetMethod(listType, typeof(List<>).GetProperty("Count").GetGetMethod()));
                    }
                    else
                    {
                        msil.Emit(OpCodes.Callvirt, listType.GetProperty("Count").GetGetMethod());
                    }

                    msil.Emit(OpCodes.Ldc_I4_0);
                    msil.Emit(OpCodes.Ceq);
                    msil.Emit(OpCodes.Brfalse, label);

                    msil.Emit(OpCodes.Ldarg_0); // this
                    msil.Emit(OpCodes.Ldnull);
                    msil.Emit(OpCodes.Stfld, fieldBuilder);

                    msil.MarkLabel(label);
                }
                msil.Emit(OpCodes.Ret);
            }

            var onSerializingBuilder = typeBuilder.DefineMethod(
                "OnSerializing",
                MethodAttributes.Private,
                null,
                new[] { typeof(StreamingContext) });
            onSerializingBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    GetConstructor<OnSerializingAttribute>(),
                    new object[0]));
            {
                var contextParam = onSerializingBuilder.DefineParameter(1, ParameterAttributes.None, "context");
                var msil = onSerializingBuilder.GetILGenerator();
                msil.Emit(OpCodes.Ldarg_0);
                msil.Emit(OpCodes.Callvirt, clearListsBuilder);
                msil.Emit(OpCodes.Ret);
            }

            var constructListsBuilder = typeBuilder.DefineMethod(
                "ConstructLists",
                MethodAttributes.Private,
                null,
                Type.EmptyTypes);
            {
                var msil = constructListsBuilder.GetILGenerator();
                foreach (var fieldBuilder in specialDefaultFieldBuilders)
                {
                    var listType = fieldBuilder.FieldType;
                    if (listType.GetGenericTypeDefinition() != typeof(List<>))
                    {
                        throw new InvalidOperationException();
                    }

                    var label = msil.DefineLabel();

                    msil.Emit(OpCodes.Ldarg_0); // this
                    msil.Emit(OpCodes.Ldfld, fieldBuilder);
                    msil.Emit(OpCodes.Ldnull);
                    msil.Emit(OpCodes.Ceq);
                    msil.Emit(OpCodes.Brfalse_S, label);

                    msil.Emit(OpCodes.Ldarg_0); // this

                    var elementType = listType.GetGenericArguments().Single();
                    if (elementType is TypeBuilder)
                    {
                        msil.Emit(OpCodes.Newobj,
                                  TypeBuilder.GetConstructor(fieldBuilder.FieldType, GetConstructor(typeof(List<>))));
                    }
                    else
                    {
                        msil.Emit(OpCodes.Newobj, GetConstructor(listType));
                    }

                    msil.Emit(OpCodes.Stfld, fieldBuilder);

                    msil.MarkLabel(label);
                }
                msil.Emit(OpCodes.Ret);
            }

            var onDeserializingBuilder = typeBuilder.DefineMethod(
                "OnDeserializing",
                MethodAttributes.Private,
                null,
                new[] { typeof(StreamingContext) });
            onDeserializingBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    GetConstructor<OnDeserializingAttribute>(),
                    new object[0]));
            {
                var contextParam = onDeserializingBuilder.DefineParameter(1, ParameterAttributes.None, "context");
                var msil = onDeserializingBuilder.GetILGenerator();
                msil.Emit(OpCodes.Ldarg_0);
                msil.Emit(OpCodes.Callvirt, constructListsBuilder);
                msil.Emit(OpCodes.Ret);
            }

            var onSerializedBuilder = typeBuilder.DefineMethod(
                "OnSerialized",
                MethodAttributes.Private,
                null,
                new[] { typeof(StreamingContext) });
            onSerializedBuilder.SetCustomAttribute(
                new CustomAttributeBuilder(
                    GetConstructor<OnSerializedAttribute>(),
                    new object[0]));
            {
                var contextParam = onSerializedBuilder.DefineParameter(1, ParameterAttributes.None, "context");
                var msil = onSerializedBuilder.GetILGenerator();
                msil.Emit(OpCodes.Ldarg_0);
                msil.Emit(OpCodes.Callvirt, constructListsBuilder);
                msil.Emit(OpCodes.Ret);
            }
        }

        private void ExportStructureFileSerialize(
            ParserSchema.Table table,
            TypeBuilder typeBuilder,
            Dictionary<ParserSchema.Column, FieldBuilder> fieldBuilders,
            Dictionary<ParserSchema.Column, Type> fieldTypes,
            Dictionary<ParserSchema.Column, FieldBuilder> polymorphBuilders)
        {
            var serializeBuilder = typeBuilder.DefineMethod(
                "Serialize",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new[] { typeof(Serialization.IFileWriter), typeof(object) });
            typeBuilder.DefineMethodOverride(
                serializeBuilder,
                typeof(Serialization.IStructure)
                    .GetMethod("Serialize", new[] { typeof(Serialization.IFileWriter), typeof(object) }));
            var writerParam = serializeBuilder.DefineParameter(1, ParameterAttributes.None, "writer");
            var stateParam = serializeBuilder.DefineParameter(2, ParameterAttributes.None, "state");

            var msil = serializeBuilder.GetILGenerator();

            // arg check
            {
                var label = msil.DefineLabel();

                msil.Emit(OpCodes.Ldarg_1);
                msil.Emit(OpCodes.Ldnull);
                msil.Emit(OpCodes.Ceq);
                msil.Emit(OpCodes.Ldc_I4_0);
                msil.Emit(OpCodes.Ceq);
                msil.Emit(OpCodes.Brtrue_S, label);

                msil.Emit(OpCodes.Ldstr, "writer");
                msil.Emit(OpCodes.Newobj, GetConstructor<ArgumentNullException>(typeof(string)));
                msil.Emit(OpCodes.Throw);

                msil.MarkLabel(label);
            }

            var checkScope = CheckScope.None;
            Label endLabel = msil.DefineLabel();

            foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
            {
                if (column.Flags.HasAllOptions(Parser.ColumnFlags.CLIENT_ONLY |
                                               Parser.ColumnFlags.SERVER_ONLY) == true)
                {
                    throw new InvalidOperationException();
                }

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.CLIENT_ONLY) == true)
                {
                    if (checkScope != CheckScope.Client)
                    {
                        if (checkScope != CheckScope.None)
                        {
                            msil.MarkLabel(endLabel);
                        }

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.Client;

                        msil.Emit(OpCodes.Ldarg_1); // writer
                        msil.Emit(OpCodes.Callvirt,
                                  typeof(Serialization.IBaseWriter).GetProperty("IsClient").GetGetMethod());
                        msil.Emit(OpCodes.Brfalse, endLabel);
                    }
                }
                else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.SERVER_ONLY) == true)
                {
                    if (checkScope != CheckScope.Server)
                    {
                        if (checkScope != CheckScope.None)
                        {
                            msil.MarkLabel(endLabel);
                        }

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.Server;

                        msil.Emit(OpCodes.Ldarg_1); // writer
                        msil.Emit(OpCodes.Callvirt,
                                  typeof(Serialization.IBaseWriter).GetProperty("IsServer").GetGetMethod());
                        msil.Emit(OpCodes.Brfalse, endLabel);
                    }
                }
                else
                {
                    if (checkScope != CheckScope.None)
                    {
                        msil.MarkLabel(endLabel);

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.None;
                    }
                }

                var token = Parser.GlobalTokens.GetToken(column.Token);
                var methodInfo = Helpers.GetWriteMethod(column);

                var basicFlags = Parser.ColumnFlags.None;
                basicFlags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.EARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.INDIRECT;

                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // writer
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // writer
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1); // writer
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                    }
                }
                else if (column.Token == Parser.TokenType.Structure) // structure
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                    }
                }
                else if (column.Token == Parser.TokenType.Polymorph) // polymorph
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                        msil.EmitConstant(column.NumberOfElements); // count
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // type
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                }
                else
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements); // count
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                    }
                }
            }

            if (checkScope != CheckScope.None)
            {
                msil.MarkLabel(endLabel);
                checkScope = CheckScope.None;
            }

            msil.Emit(OpCodes.Ret);
        }

        private void ExportStructureFileDeserialize(
            ParserSchema.Table table,
            TypeBuilder typeBuilder,
            Dictionary<ParserSchema.Column, Type> fieldTypes,
            Dictionary<ParserSchema.Column, FieldBuilder> fieldBuilders,
            Dictionary<ParserSchema.Column, FieldBuilder> polymorphBuilders)
        {
            var deserializeBuilder = typeBuilder.DefineMethod(
                "Deserialize",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new[] { typeof(Serialization.IFileReader), typeof(object) });
            typeBuilder.DefineMethodOverride(
                deserializeBuilder,
                typeof(Serialization.IStructure)
                    .GetMethod("Deserialize", new[] { typeof(Serialization.IFileReader), typeof(object) }));
            var readerParam = deserializeBuilder.DefineParameter(1, ParameterAttributes.None, "reader");
            var stateParam = deserializeBuilder.DefineParameter(2, ParameterAttributes.None, "state");

            var msil = deserializeBuilder.GetILGenerator();

            // arg check
            {
                var label = msil.DefineLabel();

                msil.Emit(OpCodes.Ldarg_1);
                msil.Emit(OpCodes.Ldnull);
                msil.Emit(OpCodes.Ceq);
                msil.Emit(OpCodes.Ldc_I4_0);
                msil.Emit(OpCodes.Ceq);
                msil.Emit(OpCodes.Brtrue_S, label);

                msil.Emit(OpCodes.Ldstr, "reader");
                msil.Emit(OpCodes.Newobj, GetConstructor<ArgumentNullException>(typeof(string)));
                msil.Emit(OpCodes.Throw);

                msil.MarkLabel(label);
            }

            var checkScope = CheckScope.None;
            Label endLabel = msil.DefineLabel();

            foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
            {
                if (column.Flags.HasAllOptions(Parser.ColumnFlags.CLIENT_ONLY |
                                               Parser.ColumnFlags.SERVER_ONLY) == true)
                {
                    throw new InvalidOperationException();
                }

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.CLIENT_ONLY) == true)
                {
                    if (checkScope != CheckScope.Client)
                    {
                        if (checkScope != CheckScope.None)
                        {
                            msil.MarkLabel(endLabel);
                        }

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.Client;

                        msil.Emit(OpCodes.Ldarg_1); // reader
                        msil.Emit(OpCodes.Callvirt,
                                  typeof(Serialization.IBaseReader).GetProperty("IsClient").GetGetMethod());
                        msil.Emit(OpCodes.Brfalse, endLabel);
                    }
                }
                else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.SERVER_ONLY) == true)
                {
                    if (checkScope != CheckScope.Server)
                    {
                        if (checkScope != CheckScope.None)
                        {
                            msil.MarkLabel(endLabel);
                        }

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.Server;

                        msil.Emit(OpCodes.Ldarg_1); // reader
                        msil.Emit(OpCodes.Callvirt,
                                  typeof(Serialization.IBaseReader).GetProperty("IsServer").GetGetMethod());
                        msil.Emit(OpCodes.Brfalse, endLabel);
                    }
                }
                else
                {
                    if (checkScope != CheckScope.None)
                    {
                        msil.MarkLabel(endLabel);

                        endLabel = msil.DefineLabel();
                        checkScope = CheckScope.None;
                    }
                }

                var token = Parser.GlobalTokens.GetToken(column.Token);
                var methodInfo = Helpers.GetReadMethod(column);

                var basicFlags = Parser.ColumnFlags.None;
                basicFlags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.EARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.INDIRECT;

                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // reader
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // reader
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // reader
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else if (column.Token == Parser.TokenType.Structure) // structure
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else if (column.Token == Parser.TokenType.Polymorph) // polymorph
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                        msil.EmitConstant(column.NumberOfElements); // count
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // type
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements); // count
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldnull); // state
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
            }

            if (checkScope != CheckScope.None)
            {
                msil.MarkLabel(endLabel);
                checkScope = CheckScope.None;
            }

            msil.Emit(OpCodes.Ret);
        }

        private void ExportStructurePacketSerialize(
            ParserSchema.Table table,
            TypeBuilder typeBuilder,
            Dictionary<ParserSchema.Column, Type> fieldTypes,
            Dictionary<ParserSchema.Column, FieldBuilder> fieldBuilders,
            Dictionary<ParserSchema.Column, FieldBuilder> polymorphBuilders)
        {
            var serializeBuilder = typeBuilder.DefineMethod(
                "Serialize",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new[] { typeof(Serialization.IPacketWriter), typeof(object) });
            typeBuilder.DefineMethodOverride(
                serializeBuilder,
                typeof(Serialization.IStructure)
                    .GetMethod("Serialize", new[] { typeof(Serialization.IPacketWriter), typeof(object) }));
            var writerParam = serializeBuilder.DefineParameter(1, ParameterAttributes.None, "writer");
            var stateParam = serializeBuilder.DefineParameter(2, ParameterAttributes.None, "state");

            var msil = serializeBuilder.GetILGenerator();
            msil.Emit(OpCodes.Newobj, GetConstructor<NotImplementedException>());
            msil.Emit(OpCodes.Throw);
        }

        private void ExportStructurePacketDeserialize(
            ParserSchema.Table table,
            TypeBuilder typeBuilder,
            Dictionary<ParserSchema.Column, Type> fieldTypes,
            Dictionary<ParserSchema.Column, FieldBuilder> fieldBuilders,
            Dictionary<ParserSchema.Column, FieldBuilder> polymorphBuilders)
        {
            var deserializeBuilder = typeBuilder.DefineMethod(
                "Deserialize",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new[] { typeof(Serialization.IPacketReader), typeof(object) });
            typeBuilder.DefineMethodOverride(
                deserializeBuilder,
                typeof(Serialization.IStructure)
                    .GetMethod("Deserialize", new[] { typeof(Serialization.IPacketReader), typeof(object) }));
            var readerParam = deserializeBuilder.DefineParameter(1, ParameterAttributes.None, "reader");
            var stateParam = deserializeBuilder.DefineParameter(2, ParameterAttributes.None, "state");

            var msil = deserializeBuilder.GetILGenerator();

            var unknownFlagLocal = msil.DeclareLocal(typeof(bool));
            var indexLocal = msil.DeclareLocal(typeof(int));

            var readHasFieldLabel = msil.DefineLabel();
            var readFieldIndexLabel = msil.DefineLabel();
            var badFieldIndex = msil.DefineLabel();
            var validFieldIndex = msil.DefineLabel();

            var switchDefaultLabel = msil.DefineLabel();
            var switchPostLabel = msil.DefineLabel();
            var returnLabel = msil.DefineLabel();

            msil.Emit(OpCodes.Ldarg_1);
            msil.Emit(OpCodes.Callvirt, typeof(Serialization.IPacketReader).GetMethod("ReadNativeBoolean"));
            msil.Emit(OpCodes.Stloc, unknownFlagLocal);

            msil.Emit(OpCodes.Ldloc, unknownFlagLocal);
            msil.Emit(OpCodes.Ldc_I4_0);
            msil.Emit(OpCodes.Ceq);
            msil.Emit(OpCodes.Brtrue_S, readHasFieldLabel);

            msil.Emit(OpCodes.Newobj, GetConstructor<FormatException>());
            msil.Emit(OpCodes.Throw);

            msil.MarkLabel(readHasFieldLabel);
            msil.Emit(OpCodes.Ldarg_1);
            msil.Emit(OpCodes.Callvirt, typeof(Serialization.IPacketReader).GetMethod("ReadNativeBoolean"));
            msil.Emit(OpCodes.Brtrue_S, readFieldIndexLabel);
            msil.Emit(OpCodes.Br, returnLabel);

            msil.MarkLabel(readFieldIndexLabel);
            msil.Emit(OpCodes.Ldarg_1);
            msil.Emit(OpCodes.Callvirt, typeof(Serialization.IPacketReader).GetMethod("ReadNativeInt32Packed"));
            msil.Emit(OpCodes.Stloc, indexLocal);

            msil.Emit(OpCodes.Ldloc, indexLocal);
            msil.Emit(OpCodes.Ldc_I4_0);
            msil.Emit(OpCodes.Blt, badFieldIndex);

            msil.Emit(OpCodes.Ldloc, indexLocal);
            msil.EmitConstant(table.Columns.Count);
            msil.Emit(OpCodes.Blt, validFieldIndex);

            msil.MarkLabel(badFieldIndex);
            msil.Emit(OpCodes.Ldstr, "invalid field index");
            msil.Emit(OpCodes.Newobj, GetConstructor<FormatException>(typeof(string)));
            msil.Emit(OpCodes.Throw);

            msil.MarkLabel(validFieldIndex);

            var jumpTable = new Label[table.Columns.Count];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                jumpTable[i] = msil.DefineLabel();
            }

            msil.Emit(OpCodes.Ldloc, indexLocal);
            msil.Emit(OpCodes.Switch, jumpTable);
            msil.Emit(OpCodes.Br, switchDefaultLabel);

            for (int i = 0; i < table.Columns.Count; i++)
            {
                msil.MarkLabel(jumpTable[i]);

                var column = table.Columns[i];

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.CLIENT_ONLY) == true)
                {
                    var okLabel = msil.DefineLabel();
                    msil.Emit(OpCodes.Ldarg_1); // reader
                    msil.Emit(OpCodes.Callvirt,
                              typeof(Serialization.IBaseReader).GetProperty("IsClient").GetGetMethod());
                    msil.Emit(OpCodes.Brtrue, okLabel);

                    msil.Emit(OpCodes.Ldstr,
                              "got field " + Helpers.GetColumnName(table, column) + ", but reader is not a client");
                    msil.Emit(OpCodes.Newobj, GetConstructor<FormatException>(typeof(string)));
                    msil.Emit(OpCodes.Throw);

                    msil.MarkLabel(okLabel);
                }

                if (column.Flags.HasAnyOptions(Parser.ColumnFlags.SERVER_ONLY) == true)
                {
                    var okLabel = msil.DefineLabel();
                    msil.Emit(OpCodes.Ldarg_1); // reader
                    msil.Emit(OpCodes.Callvirt, typeof(Serialization.IBaseReader).GetProperty("IsServer").GetGetMethod());
                    msil.Emit(OpCodes.Brtrue, okLabel);

                    msil.Emit(OpCodes.Ldstr,
                              "got field " + Helpers.GetColumnName(table, column) + ", but reader is not a server");
                    msil.Emit(OpCodes.Newobj, GetConstructor<FormatException>(typeof(string)));
                    msil.Emit(OpCodes.Throw);

                    msil.MarkLabel(okLabel);
                }

                if (column.Token == Parser.TokenType.Command)
                {
                    msil.Emit(OpCodes.Br, switchPostLabel);
                    continue;
                }

                if (Helpers.IsGoodColumn(column) == false)
                {
                    msil.Emit(OpCodes.Ldstr, "cannot serialize " + column.Name);
                    msil.Emit(OpCodes.Newobj, GetConstructor<InvalidOperationException>(typeof(string)));
                    msil.Emit(OpCodes.Throw);
                    continue;
                }

                //msil.Emit(OpCodes.Ldstr, "serializing " + typeBuilder.Name + "." + column.Name);
                //msil.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }), null);

                var token = Parser.GlobalTokens.GetToken(column.Token);
                var methodInfo = Helpers.GetReadMethod(column);

                var basicFlags = Parser.ColumnFlags.None;
                basicFlags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.EARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.INDIRECT;

                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else if (column.Token == Parser.TokenType.Structure) // structure
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt,
                                  methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]));
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else if (column.Token == Parser.TokenType.Polymorph) // polymorph
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }
                else
                {
                    if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY |
                                                   Parser.ColumnFlags.FIXED_ARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else if (column.Flags.HasAnyOptions(Parser.ColumnFlags.EARRAY) == false)
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_0); // this
                        msil.Emit(OpCodes.Ldarg_1); // parser
                        if (column.Token == Parser.TokenType.Bit)
                        {
                            msil.EmitConstant(column.BitOffset);
                        }
                        msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                      ParserSchemaFile.DefaultMaxArraySize));
                        msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                        msil.Emit(OpCodes.Not);
                        msil.Emit(OpCodes.Box, typeof(bool));
                        msil.Emit(OpCodes.Callvirt, methodInfo);
                        msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                    }
                }

                msil.Emit(OpCodes.Br, switchPostLabel);
            }

            msil.MarkLabel(switchDefaultLabel);
            msil.Emit(OpCodes.Ldstr, "cannot serialize unknown field");
            msil.Emit(OpCodes.Newobj, GetConstructor<InvalidOperationException>(typeof(string)));
            msil.Emit(OpCodes.Throw);

            msil.MarkLabel(switchPostLabel);
            msil.Emit(OpCodes.Br, readHasFieldLabel);

            msil.MarkLabel(returnLabel);
            msil.Emit(OpCodes.Ret);
        }

        private void ExportStructure(ParserSchema.Table table, TypeBuilder typeBuilder)
        {
            var fieldBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var fieldTypes = new Dictionary<ParserSchema.Column, Type>();

            var specialDefaultFieldBuilders = new List<FieldBuilder>();

            foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
            {
                var fieldType = GetColumnNativeType(typeBuilder, column);
                fieldTypes.Add(column, fieldType);

                bool isSpecialDefault;
                var fieldBuilder = ExportField(table, column, typeBuilder, out isSpecialDefault);
                fieldBuilders.Add(column, fieldBuilder);

                if (isSpecialDefault == true)
                {
                    specialDefaultFieldBuilders.Add(fieldBuilder);
                }
            }

            typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var polymorphAttributeTypes = new List<Type>();
            if (table.Columns.Any(c => c.Token == Parser.TokenType.MultiValue) == true)
            {
                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        GetConstructor<KnownTypeAttribute>(typeof(Type)),
                        new object[] { typeof(StaticVariableType) }));
                polymorphAttributeTypes.Add(typeof(StaticVariableType));
            }

            var polymorphBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var polymorphColumns = table.Columns
                                        .Where(c => Helpers.IsGoodColumn(c) && c.Token == Parser.TokenType.Polymorph)
                                        .ToArray();
            if (polymorphColumns.Length > 0)
            {
                var cctorBuilder = typeBuilder.DefineTypeInitializer();
                var cctorMsil = cctorBuilder.GetILGenerator();
                var typeList = cctorMsil.DeclareLocal(typeof(Type[]));

                foreach (var column in polymorphColumns)
                {
                    var fieldBuilder = typeBuilder.DefineField(
                        "_ValidFor" + Helpers.GetColumnName(table, column),
                        typeof(Type[]),
                        FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
                    polymorphBuilders.Add(column, fieldBuilder);
                }

                foreach (var column in polymorphColumns)
                {
                    var fieldBuilder = polymorphBuilders[column];

                    cctorMsil.EmitConstant(column.Subtable.Columns.Count);
                    cctorMsil.Emit(OpCodes.Newarr, typeof(Type));
                    cctorMsil.Emit(OpCodes.Stloc, typeList);

                    for (int i = 0; i < column.Subtable.Columns.Count; i++)
                    {
                        var subcolumn = column.Subtable.Columns[i];

                        cctorMsil.Emit(OpCodes.Ldloc, typeList);
                        cctorMsil.EmitConstant(i);

                        cctorMsil.Emit(OpCodes.Ldtoken, this.GetColumnNativeType(typeBuilder, subcolumn));
                        cctorMsil.Emit(OpCodes.Call,
                                       typeof(Type).GetMethod("GetTypeFromHandle",
                                                              new[] { typeof(RuntimeTypeHandle) }));

                        cctorMsil.Emit(OpCodes.Stelem_Ref);
                    }

                    cctorMsil.Emit(OpCodes.Ldloc, typeList);
                    cctorMsil.Emit(OpCodes.Stsfld, fieldBuilder);
                }

                foreach (var column in polymorphColumns)
                {
                    foreach (var subcolumn in column.Subtable.Columns)
                    {
                        var type = this.GetColumnNativeType(typeBuilder, subcolumn);
                        if (polymorphAttributeTypes.Contains(type) == true)
                        {
                            continue;
                        }
                        polymorphAttributeTypes.Add(type);

                        typeBuilder.SetCustomAttribute(
                            new CustomAttributeBuilder(
                                GetConstructor<KnownTypeAttribute>(typeof(Type)),
                                new object[] { type }));
                    }
                }

                cctorMsil.Emit(OpCodes.Ret);
            }

            if (specialDefaultFieldBuilders.Count > 0)
            {
                this.ExportStructureSpecialDefaults(typeBuilder, specialDefaultFieldBuilders);
            }

            this.ExportStructureFileSerialize(table, typeBuilder, fieldBuilders, fieldTypes, polymorphBuilders);
            this.ExportStructureFileDeserialize(table, typeBuilder, fieldTypes, fieldBuilders, polymorphBuilders);
            this.ExportStructurePacketSerialize(table, typeBuilder, fieldTypes, fieldBuilders, polymorphBuilders);
            this.ExportStructurePacketDeserialize(table, typeBuilder, fieldTypes, fieldBuilders, polymorphBuilders);
        }
    }
}
