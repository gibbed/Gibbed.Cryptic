/* Copyright (c) 2012 Rick (rick 'at' gibbed 'dot' us)
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
        private readonly ParserLoader _ParserLoader;
        private readonly EnumLoader _EnumLoader;

        public Generator(TargetGame targetGame, ParserLoader parserLoader, EnumLoader enumLoader)
        {
            this._TargetGame = targetGame;
            this._ParserLoader = parserLoader;
            this._EnumLoader = enumLoader;
        }

        private static MethodInfo _EnumTryParse =
            typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .First(m => m.Name == "TryParse" && m.GetParameters().Length == 3);

        public void ExportAssembly(string outputPath, string version)
        {
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
                    new CustomAttributeBuilder(typeof(AssemblyDescriptionAttribute)
                                                   .GetConstructor(new[] { typeof(string) }),
                                               new object[] { version }));
            }

            this._AssemblyBuilder.DefineVersionInfoResource();

            this._ModuleBuilder = this._AssemblyBuilder.DefineDynamicModule(
                assemblyName.Name,
                Path.GetFileName(outputPath));

            this._EnumTypes = new Dictionary<ParserSchema.Enumeration, Type>();
            this._TableTypes = new Dictionary<ParserSchema.Table, TypeBuilder>();

            var queue = new Queue<QueuedType>();
            var done = new List<string>();

            queue.Clear();
            done.Clear();

            Console.WriteLine("Loading parsers...");
            foreach (var name in this._ParserLoader.ParserNames)
            {
                queue.Enqueue(new QueuedType(
                                  name,
                                  this._ParserLoader.LoadParser(name).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                Console.WriteLine("Generating type for {0}...", qt.Key);

                TypeBuilder typeBuilder;

                if (qt.Parent == null)
                {
                    typeBuilder = this._ModuleBuilder.DefineType(
                        "Gibbed." + this._TargetGame.ToString() + ".Serialization." + qt.Name,
                        TypeAttributes.Public,
                        null,
                        new[] { typeof(Serialization.IStructure) });
                }
                else
                {
                    typeBuilder = this._TableTypes[qt.Parent.Table].DefineNestedType(
                        qt.Name,
                        TypeAttributes.NestedPublic,
                        null,
                        new[] { typeof(Serialization.IStructure) });
                }

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataContractAttribute)
                                                   .GetConstructor(Type.EmptyTypes),
                                               new object[] { },
                                               new[] { typeof(DataContractAttribute).GetProperty("Namespace") },
                                               new object[]
                                               {
                                                   "http://datacontract.gib.me/" +
                                                   this._TargetGame.ToString().ToLowerInvariant()
                                               }));

                this._TableTypes.Add(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if (column.Token == 20)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queue.Any(e => e.Name == column.SubtableExternalName && e.Parent == null) == false)
                            {
                                queue.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = qt.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queue.Any(e => e.Name == column.Name && e.Parent == qt) == false)
                            {
                                queue.Enqueue(new QueuedType(column.Name, column.Subtable, qt));
                            }
                        }
                    }
                    else if (column.Token == 21)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.SubtableIsExternal == true)
                            {
                                if (done.Contains(subcolumn.SubtableExternalName) == false &&
                                    queue.Any(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null) ==
                                    false)
                                {
                                    queue.Enqueue(new QueuedType(subcolumn.SubtableExternalName, subcolumn.Subtable));
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

            queue.Clear();
            done.Clear();

            foreach (var name in this._ParserLoader.ParserNames)
            {
                queue.Enqueue(new QueuedType(
                                  name,
                                  this._ParserLoader.LoadParser(name).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                Console.WriteLine("Generating code for {0}...", qt.Key);

                var typeBuilder = this._TableTypes[qt.Table];

                this.ExportStructure(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if (column.Token == 20)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queue.Any(e => e.Name == column.SubtableExternalName && e.Parent == null) == false)
                            {
                                queue.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = qt.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queue.Any(e => e.Name == column.Name && e.Parent == qt) == false)
                            {
                                queue.Enqueue(new QueuedType(column.Name, column.Subtable, qt));
                            }
                        }
                    }
                    else if (column.Token == 21)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.SubtableIsExternal == true)
                            {
                                if (done.Contains(subcolumn.SubtableExternalName) == false &&
                                    queue.Count(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null) == 0)
                                {
                                    queue.Enqueue(new QueuedType(subcolumn.SubtableExternalName, subcolumn.Subtable));
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

                if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                {
                    return e;
                }

                if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                {
                    return e.MakeArrayType();
                }

                return typeof(List<>).MakeGenericType(e);
            }

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                switch (column.Token)
                {
                    case 3:
                        return typeof(byte);
                    case 4:
                        return typeof(short);
                    case 5:
                        return typeof(int);
                    case 6:
                        return typeof(long);
                    case 7:
                        return typeof(float);
                    case 8:
                        return typeof(string);
                    case 9:
                        return typeof(string);
                    case 10:
                        return typeof(int);
                    case 11:
                        return typeof(int);
                    case 12:
                        return typeof(bool);
                    case 14:
                        return typeof(bool);
                    case 16:
                        return typeof(MATPYR);
                    case 17:
                        return typeof(string);
                    case 18:
                        return typeof(string);
                    case 20:
                        return this._TableTypes[column.Subtable];
                    case 21:
                        return typeof(object);
                    case 22:
                        return typeof(StashTable);
                    case 23:
                        return typeof(uint);
                    case 24:
                        return typeof(MultiValue);
                    case 25:
                        return typeof(string);
                    default:
                        throw new NotImplementedException();
                }
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                switch (column.Token)
                {
                    case 3:
                        return typeof(byte[]);
                    case 4:
                        return typeof(short[]);
                    case 5:
                        return typeof(int[]);
                    case 6:
                        return typeof(long[]);
                    case 7:
                        return typeof(float[]);
                    case 8:
                        return typeof(string[]);
                    case 9:
                        return typeof(string[]);
                    case 15:
                        return typeof(float[]);
                    case 16:
                        return typeof(MATPYR[]);
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                switch (column.Token)
                {
                    case 3:
                        return typeof(List<byte>);
                    case 4:
                        return typeof(List<short>);
                    case 5:
                        return typeof(List<int>);
                    case 6:
                        return typeof(List<long>);
                    case 7:
                        return typeof(List<float>);
                    case 8:
                        return typeof(List<string>);
                    case 9:
                        return typeof(List<string>);
                    case 16:
                        return typeof(List<MATPYR>);
                    case 17:
                        return typeof(List<string>);
                    case 19:
                        return typeof(List<FunctionCall>);
                    case 20:
                        return typeof(List<>).MakeGenericType(this._TableTypes[column.Subtable]);
                    case 21:
                        return typeof(List<object>);
                    case 24:
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
                    case 3:
                        underlyingType = typeof(byte);
                        break;
                    case 4:
                        underlyingType = typeof(short);
                        break;
                    case 5:
                        underlyingType = typeof(int);
                        break;
                    case 23:
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
                    case 5:
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
                var name = column.StaticDefineListExternalName;
                var builder = this._ModuleBuilder.DefineEnum(
                    "Gibbed." + this._TargetGame.ToString() + ".Serialization.StaticDefineList." +
                    column.StaticDefineListExternalName,
                    TypeAttributes.Public,
                    underlyingType);

                if (column.Format == ParserSchema.ColumnFormat.Flags ||
                    column.Name.ToLowerInvariant().Contains("flag") == true)
                {
                    builder.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof(FlagsAttribute)
                                                       .GetConstructor(Type.EmptyTypes),
                                                   new object[] { }));
                }

                foreach (var kv in e.Elements)
                {
                    var value = ChangeEnumType(int.Parse(kv.Value), underlyingType);
                    var fb = builder.DefineLiteral(
                        kv.Key,
                        value);
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
                        new CustomAttributeBuilder(typeof(FlagsAttribute)
                                                       .GetConstructor(Type.EmptyTypes),
                                                   new object[] { }));
                }

                builder.DefineField(
                    "value__",
                    typeof(int),
                    FieldAttributes.Private | FieldAttributes.SpecialName);

                foreach (var kv in e.Elements)
                {
                    var value = ChangeEnumType(int.Parse(kv.Value), underlyingType);

                    var fb = builder.DefineField(
                        kv.Key,
                        builder,
                        FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                    fb.SetConstant(value);
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
                "Gibbed." + this._TargetGame.ToString() + ".Serialization.Fields." + structure.Name + "Field",
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
            TypeBuilder structure)
        {
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
                    new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                                   .GetConstructor(Type.EmptyTypes),
                                               new object[] { },
                                               new[]
                                               {
                                                   typeof(DataMemberAttribute).GetProperty("Name"),
                                                   typeof(DataMemberAttribute).GetProperty("Order"),
                                               },
                                               new object[] { name, table.Columns.IndexOf(column) }));

                wrapperBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(CollectionDataContractAttribute)
                                                   .GetConstructor(Type.EmptyTypes),
                                               new object[] { },
                                               new[]
                                               {
                                                   typeof(CollectionDataContractAttribute).GetProperty("Name"),
                                                   typeof(CollectionDataContractAttribute).GetProperty("ItemName"),
                                               },
                                               new object[] { structure.Name + "SetField", "Field" }));
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
                    new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                                   .GetConstructor(Type.EmptyTypes),
                                               new object[] { },
                                               new[]
                                               {
                                                   typeof(DataMemberAttribute).GetProperty("Name"),
                                                   typeof(DataMemberAttribute).GetProperty("Order"),
                                                   typeof(DataMemberAttribute).GetProperty("EmitDefaultValue"),
                                               },
                                               new object[] { name, table.Columns.IndexOf(column), false }));
            }
            else if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                     column.StaticDefineList != null)
            {
                var wrapperName = "__wrapper_" + name;

                if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
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
                        new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                                       .GetConstructor(Type.EmptyTypes),
                                                   new object[] { },
                                                   new[]
                                                   {
                                                       typeof(DataMemberAttribute).GetProperty("Name"),
                                                       typeof(DataMemberAttribute).GetProperty("Order"),
                                                   },
                                                   new object[] { name, table.Columns.IndexOf(column) }));
                }
                else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                {
                    throw new NotImplementedException();
                }
                else
                {
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
                               typeof(EnumParser<>).MakeGenericType(type.GetGenericArguments()[0])
                                                   .GetMethod("ToStringList"));
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
                    setIL.Emit(OpCodes.Call,
                               typeof(EnumParser<>).MakeGenericType(type.GetGenericArguments()[0])
                                                   .GetMethod("FromStringList"));
                    setIL.Emit(OpCodes.Stfld, builder);
                    setIL.Emit(OpCodes.Ret);

                    wrapperBuilder.SetGetMethod(getBuilder);
                    wrapperBuilder.SetSetMethod(setBuilder);

                    wrapperBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                                       .GetConstructor(Type.EmptyTypes),
                                                   new object[] { },
                                                   new[]
                                                   {
                                                       typeof(DataMemberAttribute).GetProperty("Name"),
                                                       typeof(DataMemberAttribute).GetProperty("Order"),
                                                   },
                                                   new object[] { name, table.Columns.IndexOf(column) }));
                }
            }
            else
            {
                builder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                                   .GetConstructor(Type.EmptyTypes),
                                               new object[] { },
                                               new[]
                                               {
                                                   typeof(DataMemberAttribute).GetProperty("Order"),
                                               },
                                               new object[] { table.Columns.IndexOf(column) }));
            }

            return builder;
        }

        private void ExportStructure(ParserSchema.Table table, TypeBuilder typeBuilder)
        {
            var fieldBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var fieldTypes = new Dictionary<ParserSchema.Column, Type>();

            foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
            {
                var fieldType = GetColumnNativeType(typeBuilder, column);
                fieldTypes.Add(column, fieldType);

                var fieldBuilder = ExportField(table, column, typeBuilder);
                fieldBuilders.Add(column, fieldBuilder);
            }

            var ctorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var polymorphAttributeTypes = new List<Type>();
            if (table.Columns.Any(c => c.Token == 24) == true)
            {
                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(KnownTypeAttribute)
                                                   .GetConstructor(new[] { typeof(Type) }),
                                               new object[] { typeof(StaticVariableType) }));
                polymorphAttributeTypes.Add(typeof(StaticVariableType));
            }

            var polymorphBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var polymorphColumns = table.Columns.Where(c => Helpers.IsGoodColumn(c) && c.Token == 21).ToArray();
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
                            new CustomAttributeBuilder(typeof(KnownTypeAttribute)
                                                           .GetConstructor(new[] { typeof(Type) }),
                                                       new object[] { type }));
                    }
                }

                cctorMsil.Emit(OpCodes.Ret);
            }

            // IFileReader.Serialize
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
                    msil.Emit(OpCodes.Newobj,
                              typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }));
                    msil.Emit(OpCodes.Throw);

                    msil.MarkLabel(label);
                }

                var checkScope = CheckScope.None;
                Label endLabel = msil.DefineLabel();

                foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if ((column.Flags & Parser.ColumnFlags.CLIENT_ONLY) != 0 &&
                        (column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((column.Flags & Parser.ColumnFlags.CLIENT_ONLY) != 0)
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
                            msil.EmitCall(OpCodes.Callvirt,
                                          typeof(Serialization.IBaseWriter).GetProperty("IsClient").GetGetMethod(),
                                          Type.EmptyTypes);
                            msil.Emit(OpCodes.Brfalse, endLabel);
                        }
                    }
                    else if ((column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
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
                            msil.EmitCall(OpCodes.Callvirt,
                                          typeof(Serialization.IBaseWriter).GetProperty("IsServer").GetGetMethod(),
                                          Type.EmptyTypes);
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
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // writer
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // writer
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_1); // writer
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                        }
                    }
                    else if (column.Token == 20) // structure
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                        }
                    }
                    else if (column.Token == 21) // polymorph
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                            msil.EmitConstant(column.NumberOfElements); // count
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
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
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                        }
                    }
                    else
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements); // count
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldfld, fieldBuilders[column]);
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
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

            // IFileReader.Deserialize
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
                    msil.Emit(OpCodes.Newobj,
                              typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }));
                    msil.Emit(OpCodes.Throw);

                    msil.MarkLabel(label);
                }

                var checkScope = CheckScope.None;
                Label endLabel = msil.DefineLabel();

                foreach (var column in table.Columns.Where(Helpers.IsGoodColumn))
                {
                    if ((column.Flags & Parser.ColumnFlags.CLIENT_ONLY) != 0 &&
                        (column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
                    {
                        throw new InvalidOperationException();
                    }

                    if ((column.Flags & Parser.ColumnFlags.CLIENT_ONLY) != 0)
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
                            msil.EmitCall(OpCodes.Callvirt,
                                          typeof(Serialization.IBaseReader).GetProperty("IsClient").GetGetMethod(),
                                          Type.EmptyTypes);
                            msil.Emit(OpCodes.Brfalse, endLabel);
                        }
                    }
                    else if ((column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
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
                            msil.EmitCall(OpCodes.Callvirt,
                                          typeof(Serialization.IBaseReader).GetProperty("IsServer").GetGetMethod(),
                                          Type.EmptyTypes);
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
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // reader
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // reader
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // reader
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else if (column.Token == 20) // structure
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else if (column.Token == 21) // polymorph
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]); // types
                            msil.EmitConstant(column.NumberOfElements); // count
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
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
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements); // count
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldnull); // state
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
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

            // IPacketReader.Serialize
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
                msil.Emit(OpCodes.Newobj, typeof(NotImplementedException).GetConstructor(Type.EmptyTypes));
                msil.Emit(OpCodes.Throw);
            }

            // IPacketReader.Deserialize
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

                msil.Emit(OpCodes.Newobj, typeof(FormatException).GetConstructor(Type.EmptyTypes));
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
                msil.Emit(OpCodes.Newobj, typeof(FormatException).GetConstructor(new[] { typeof(string) }));
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

                    if ((column.Flags & Parser.ColumnFlags.CLIENT_ONLY) != 0)
                    {
                        var okLabel = msil.DefineLabel();
                        msil.Emit(OpCodes.Ldarg_1); // reader
                        msil.EmitCall(OpCodes.Callvirt,
                                      typeof(Serialization.IBaseReader).GetProperty("IsClient").GetGetMethod(),
                                      Type.EmptyTypes);
                        msil.Emit(OpCodes.Brtrue, okLabel);

                        msil.Emit(OpCodes.Ldstr,
                                  "got field " + Helpers.GetColumnName(table, column) + ", but reader is not a client");
                        msil.Emit(OpCodes.Newobj, typeof(FormatException).GetConstructor(new[] { typeof(string) }));
                        msil.Emit(OpCodes.Throw);

                        msil.MarkLabel(okLabel);
                    }

                    if ((column.Flags & Parser.ColumnFlags.SERVER_ONLY) != 0)
                    {
                        var okLabel = msil.DefineLabel();
                        msil.Emit(OpCodes.Ldarg_1); // reader
                        msil.EmitCall(OpCodes.Callvirt,
                                      typeof(Serialization.IBaseReader).GetProperty("IsServer").GetGetMethod(),
                                      Type.EmptyTypes);
                        msil.Emit(OpCodes.Brtrue, okLabel);

                        msil.Emit(OpCodes.Ldstr,
                                  "got field " + Helpers.GetColumnName(table, column) + ", but reader is not a server");
                        msil.Emit(OpCodes.Newobj, typeof(FormatException).GetConstructor(new[] { typeof(string) }));
                        msil.Emit(OpCodes.Throw);

                        msil.MarkLabel(okLabel);
                    }

                    if (column.Token == 25)
                    {
                        msil.Emit(OpCodes.Br, switchPostLabel);
                        continue;
                    }

                    if (Helpers.IsGoodColumn(column) == false)
                    {
                        msil.Emit(OpCodes.Ldstr, "cannot serialize " + column.Name);
                        msil.Emit(OpCodes.Newobj,
                                  typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
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
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else if (column.Token == 20) // structure
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
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
                            msil.EmitCall(OpCodes.Callvirt,
                                          methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]),
                                          null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else if (column.Token == 21) // polymorph
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                            msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
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
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }
                    else
                    {
                        if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.NumberOfElements);
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                        else
                        {
                            msil.Emit(OpCodes.Ldarg_0); // this
                            msil.Emit(OpCodes.Ldarg_1); // parser
                            if (column.Token == 23)
                            {
                                msil.EmitConstant(column.BitOffset);
                            }
                            msil.EmitConstant(column.GetFormatStringAsInt("MAX_ARRAY_SIZE",
                                                                          ParserSchemaFile.DefaultMaxArraySize));
                            msil.Emit(OpCodes.Ldloc, unknownFlagLocal); // state
                            msil.Emit(OpCodes.Not);
                            msil.Emit(OpCodes.Box, typeof(bool));
                            msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                            msil.Emit(OpCodes.Stfld, fieldBuilders[column]);
                        }
                    }

                    msil.Emit(OpCodes.Br, switchPostLabel);
                }

                msil.MarkLabel(switchDefaultLabel);
                msil.Emit(OpCodes.Ldstr, "cannot serialize unknown field");
                msil.Emit(OpCodes.Newobj,
                          typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }));
                msil.Emit(OpCodes.Throw);

                msil.MarkLabel(switchPostLabel);
                msil.Emit(OpCodes.Br, readHasFieldLabel);

                msil.MarkLabel(returnLabel);
                msil.Emit(OpCodes.Ret);
            }
        }
    }
}
