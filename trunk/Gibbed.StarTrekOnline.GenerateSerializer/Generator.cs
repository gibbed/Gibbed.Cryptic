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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using Gibbed.Cryptic.FileFormats;
using Parser = Gibbed.Cryptic.FileFormats.Parser;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    internal class Generator
    {
        private AssemblyBuilder AssemblyBuilder;
        private ModuleBuilder ModuleBuilder;

        private ParserLoader ParserLoader;
        private EnumLoader EnumLoader;

        public Generator(ParserLoader parserLoader, EnumLoader enumLoader)
        {
            this.ParserLoader = parserLoader;
            this.EnumLoader = enumLoader;
        }

        private static MethodInfo EnumTryParse =
            typeof(Enum).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "TryParse" && m.GetParameters().Length == 3)
            .First();

        public void Export(string outputPath, string version)
        {
            var assemblyName = new AssemblyName();
            assemblyName.Name = Path.GetFileNameWithoutExtension(outputPath);

            this.AssemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Save);

            if (version != null)
            {
                this.AssemblyBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(AssemblyDescriptionAttribute)
                        .GetConstructor(new Type[] { typeof(string) }), new object[] { version }));
            }

            this.AssemblyBuilder.DefineVersionInfoResource();

            this.ModuleBuilder = this.AssemblyBuilder.DefineDynamicModule(
                assemblyName.Name, Path.GetFileName(outputPath));

            this.EnumTypes = new Dictionary<ParserSchema.Enumeration, Type>();
            this.TableTypes = new Dictionary<ParserSchema.Table, TypeBuilder>();

            var queue = new Queue<QueuedType>();
            var done = new List<string>();

            queue.Clear();
            done.Clear();
            
            foreach (var name in this.ParserLoader.ParserNames)
            {
                queue.Enqueue(new QueuedType(
                    name,
                    this.ParserLoader.LoadParser(name).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                TypeBuilder typeBuilder;

                if (qt.Parent == null)
                {
                    typeBuilder = this.ModuleBuilder.DefineType(
                        "Gibbed.StarTrekOnline.Serialization." + qt.Name,
                        TypeAttributes.Public,
                        null,
                        new Type[] { typeof(ICrypticStructure) });
                }
                else
                {
                    typeBuilder = this.TableTypes[qt.Parent.Table].DefineNestedType(
                        qt.Name,
                        TypeAttributes.NestedPublic,
                        null,
                        new Type[] { typeof(ICrypticStructure) });
                }

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataContractAttribute)
                        .GetConstructor(Type.EmptyTypes), new object[] { }));

                this.TableTypes.Add(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns
                    .Where(c => Helpers.IsGoodColumn(c)))
                {
                    if (column.Token == 20)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queue.Where(e => e.Name == column.SubtableExternalName && e.Parent == null).Count() == 0)
                            {
                                queue.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = qt.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queue.Where(e => e.Name == column.Name && e.Parent == qt).Count() == 0)
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
                                    queue.Where(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null).Count() == 0)
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

            foreach (var name in this.ParserLoader.ParserNames)
            {
                queue.Enqueue(new QueuedType(
                    name, this.ParserLoader.LoadParser(name).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                var typeBuilder = this.TableTypes[qt.Table];

                this.GenerateTable(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns
                    .Where(c => Helpers.IsGoodColumn(c)))
                {
                    if (column.Token == 20)
                    {
                        if (column.SubtableIsExternal == true)
                        {
                            if (done.Contains(column.SubtableExternalName) == false &&
                                queue.Where(e => e.Name == column.SubtableExternalName && e.Parent == null).Count() == 0)
                            {
                                queue.Enqueue(new QueuedType(column.SubtableExternalName, column.Subtable));
                            }
                        }
                        else
                        {
                            var key = qt.Key + "." + column.Name;

                            if (done.Contains(key) == false &&
                                queue.Where(e => e.Name == column.Name && e.Parent == qt).Count() == 0)
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
                                    queue.Where(e => e.Name == subcolumn.SubtableExternalName && e.Parent == null).Count() == 0)
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

            this.AssemblyBuilder.Save(outputPath);
        }

        private Dictionary<ParserSchema.Table, TypeBuilder> TableTypes;
        private Dictionary<ParserSchema.Enumeration, Type> EnumTypes;

        private Type GetColumnNativeType(
            TypeBuilder parent,
            ParserSchema.Column column)
        {
            if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                column.StaticDefineList != null)
            {
                Type e;

                if (column.StaticDefineListIsExternal == true)
                {
                    e = this.GenerateEnum(parent, column, this.EnumLoader.LoadEnum(column.StaticDefineListExternalName).Enum);
                }
                else
                {
                    e = this.GenerateEnum(parent, column, column.StaticDefineList);
                }

                if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                {
                    return e;
                }
                else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                {
                    return e.MakeArrayType();
                }
                else
                {
                    return typeof(List<>).MakeGenericType(e);
                }
            }

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                switch (column.Token)
                {
                    case 3: return typeof(byte);
                    case 4: return typeof(short);
                    case 5: return typeof(int);
                    case 6: return typeof(long);
                    case 7: return typeof(float);
                    case 8: return typeof(string);
                    case 9: return typeof(string);
                    case 10: return typeof(int);
                    case 11: return typeof(int);
                    case 12: return typeof(bool);
                    case 14: return typeof(bool);
                    case 16: return typeof(MATPYR);
                    case 17: return typeof(string);
                    case 18: return typeof(string);
                    case 20: return this.TableTypes[column.Subtable];
                    case 21: return typeof(object);
                    case 22: return typeof(StashTable);
                    case 23: return typeof(uint);
                    case 24: return typeof(MultiValue);
                    case 25: return typeof(string);
                    default: throw new NotImplementedException();
                }
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                switch (column.Token)
                {
                    case 3: return typeof(byte[]);
                    case 4: return typeof(short[]);
                    case 5: return typeof(int[]);
                    case 6: return typeof(long[]);
                    case 7: return typeof(float[]);
                    case 8: return typeof(string[]);
                    case 9: return typeof(string[]);
                    case 15: return typeof(QUATPYR[]);
                    case 16: return typeof(MATPYR[]);
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                switch (column.Token)
                {
                    case 3: return typeof(List<byte>);
                    case 4: return typeof(List<short>);
                    case 5: return typeof(List<int>);
                    case 6: return typeof(List<long>);
                    case 7: return typeof(List<float>);
                    case 8: return typeof(List<string>);
                    case 9: return typeof(List<string>);
                    case 16: return typeof(List<MATPYR>);
                    case 17: return typeof(List<string>);
                    case 19: return typeof(List<FunctionCall>);
                    case 20: return typeof(List<>).MakeGenericType(this.TableTypes[column.Subtable]);
                    case 21: return typeof(List<object>);
                    case 24: return typeof(List<MultiValue>);
                    default: throw new NotImplementedException();
                }
            }
        }

        private Type GenerateEnum(
            TypeBuilder parent,
            ParserSchema.Column column,
            ParserSchema.Enumeration e)
        {
            if (this.EnumTypes.ContainsKey(e) == true)
            {
                return this.EnumTypes[e];
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
                    case 3: underlyingType = typeof(byte); break;
                    case 4: underlyingType = typeof(short); break;
                    case 5: underlyingType = typeof(int); break;
                    case 23: underlyingType = typeof(uint); break;
                    default: throw new NotSupportedException();
                }
            }
            else if (column.Format == ParserSchema.ColumnFormat.Color)
            {
                switch (column.Token)
                {
                    case 5: underlyingType = typeof(uint); break;
                    default: throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            if (column.StaticDefineListIsExternal == true)
            {
                var name = column.StaticDefineListExternalName;
                var builder = this.ModuleBuilder.DefineEnum(
                    "Gibbed.StarTrekOnline.Serialization.StaticDefineList." + column.StaticDefineListExternalName,
                    TypeAttributes.Public,
                    underlyingType);

                if (column.Format == ParserSchema.ColumnFormat.Flags ||
                    column.Name.ToLowerInvariant().Contains("flag") == true)
                {
                    builder.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof(FlagsAttribute)
                            .GetConstructor(Type.EmptyTypes), new object[] { }));
                }

                foreach (var kv in e.Elements)
                {
                    var value = Convert.ChangeType(int.Parse(kv.Value), underlyingType);
                    var fb = builder.DefineLiteral(
                        kv.Key,
                        value);
                }

                var type = builder.CreateType();
                this.EnumTypes.Add(e, type);
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
                            .GetConstructor(Type.EmptyTypes), new object[] { }));
                }

                builder.DefineField(
                    "value__",
                    typeof(int),
                    FieldAttributes.Private | FieldAttributes.SpecialName);

                foreach (var kv in e.Elements)
                {
                    var value = Convert.ChangeType(int.Parse(kv.Value), underlyingType);

                    var fb = builder.DefineField(
                        kv.Key,
                        builder,
                        FieldAttributes.Public | FieldAttributes.Literal | FieldAttributes.Static);
                    fb.SetConstant(value);
                }

                var type = builder.CreateType();
                this.EnumTypes.Add(e, type);
                return type;
            }
        }

        private void GenerateTable(ParserSchema.Table table, TypeBuilder typeBuilder)
        {
            var fieldBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var fieldTypes = new Dictionary<ParserSchema.Column, Type>();

            foreach (var column in table.Columns
                .Where(c => Helpers.IsGoodColumn(c)))
            {
                var fieldName = Helpers.GetColumnName(column);
                var fieldType = GetColumnNativeType(typeBuilder, column);
                fieldTypes.Add(column, fieldType);

                var fieldBuilder = typeBuilder.DefineField(
                    fieldName,
                    fieldType,
                    FieldAttributes.Public);

                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    var wrapperName = "__wrapper_" + fieldName;

                    if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                    {
                        var wrapperBuilder = typeBuilder.DefineProperty(
                            wrapperName,
                            PropertyAttributes.None,
                            typeof(string),
                            Type.EmptyTypes);

                        var getBuilder = typeBuilder.DefineMethod(
                            "get_" + wrapperName,
                            MethodAttributes.Public |
                            MethodAttributes.SpecialName |
                            MethodAttributes.HideBySig,
                            typeof(string),
                            Type.EmptyTypes);

                        var getIL = getBuilder.GetILGenerator();
                        getIL.Emit(OpCodes.Ldarg_0);
                        getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                        getIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(fieldType).GetMethod("ToStringValue"));
                        getIL.Emit(OpCodes.Ret);

                        var setBuilder = typeBuilder.DefineMethod(
                            "set_" + wrapperName,
                            MethodAttributes.Public |
                            MethodAttributes.SpecialName |
                            MethodAttributes.HideBySig,
                            null,
                            new Type[] { typeof(string) });

                        var setIL = setBuilder.GetILGenerator();
                        setIL.Emit(OpCodes.Ldarg_0);
                        setIL.Emit(OpCodes.Ldarg_1);
                        setIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(fieldType).GetMethod("FromStringValue"));
                        setIL.Emit(OpCodes.Stfld, fieldBuilder);
                        setIL.Emit(OpCodes.Ret);

                        wrapperBuilder.SetGetMethod(getBuilder);
                        wrapperBuilder.SetSetMethod(setBuilder);

                        wrapperBuilder.SetCustomAttribute(
                            new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                .GetConstructor(Type.EmptyTypes), new object[] { },
                                new PropertyInfo[] { typeof(DataMemberAttribute).GetProperty("Name") },
                                new object[] { fieldName }));
                    }
                    else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var wrapperBuilder = typeBuilder.DefineProperty(
                            wrapperName,
                            PropertyAttributes.None,
                            typeof(List<string>),
                            Type.EmptyTypes);

                        var getBuilder = typeBuilder.DefineMethod(
                            "get_" + wrapperName,
                            MethodAttributes.Public |
                            MethodAttributes.SpecialName |
                            MethodAttributes.HideBySig,
                            typeof(List<string>),
                            Type.EmptyTypes);

                        var getIL = getBuilder.GetILGenerator();
                        getIL.Emit(OpCodes.Ldarg_0);
                        getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                        getIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(fieldType.GetGenericArguments()[0]).GetMethod("ToStringList"));
                        getIL.Emit(OpCodes.Ret);

                        var setBuilder = typeBuilder.DefineMethod(
                            "set_" + wrapperName,
                            MethodAttributes.Public |
                            MethodAttributes.SpecialName |
                            MethodAttributes.HideBySig,
                            null,
                            new Type[] { typeof(string) });

                        var setIL = setBuilder.GetILGenerator();
                        setIL.Emit(OpCodes.Ldarg_0);
                        setIL.Emit(OpCodes.Ldarg_1);
                        setIL.Emit(OpCodes.Call, typeof(EnumParser<>).MakeGenericType(fieldType.GetGenericArguments()[0]).GetMethod("FromStringList"));
                        setIL.Emit(OpCodes.Stfld, fieldBuilder);
                        setIL.Emit(OpCodes.Ret);

                        wrapperBuilder.SetGetMethod(getBuilder);
                        wrapperBuilder.SetSetMethod(setBuilder);

                        wrapperBuilder.SetCustomAttribute(
                            new CustomAttributeBuilder(typeof(DataMemberAttribute)
                                .GetConstructor(Type.EmptyTypes), new object[] { },
                                new PropertyInfo[] { typeof(DataMemberAttribute).GetProperty("Name") },
                                new object[] { fieldName }));
                    }
                }
                else
                {
                    fieldBuilder.SetCustomAttribute(
                        new CustomAttributeBuilder(typeof(DataMemberAttribute)
                            .GetConstructor(Type.EmptyTypes), new object[] { }));
                }

                fieldBuilders.Add(column, fieldBuilder);
            }

            var ctorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var polymorphAttributeTypes = new List<Type>();

            var polymorphBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var polymorphColumns = table.Columns.Where(c => Helpers.IsGoodColumn(c) && c.Token == 21);
            if (polymorphColumns.Count() > 0)
            {
                var cctorBuilder = typeBuilder.DefineTypeInitializer();
                var cctorMsil = cctorBuilder.GetILGenerator();
                var typeList = cctorMsil.DeclareLocal(typeof(Type[]));

                foreach (var column in polymorphColumns)
                {
                    var fieldBuilder = typeBuilder.DefineField(
                        "_ValidFor" + Helpers.GetColumnName(column),
                        typeof(Type[]),
                        FieldAttributes.Private | FieldAttributes.Static);
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
                        cctorMsil.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
                        
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
                            .GetConstructor(new Type[] { typeof(Type) }), new object[] { type }));
                    }
                }

                cctorMsil.Emit(OpCodes.Ret);
            }

            var serializeBuilder = typeBuilder.DefineMethod(
                "Serialize",
                MethodAttributes.Public | MethodAttributes.Virtual,
                null,
                new Type[] { typeof(ICrypticStream) });
            var serializeDeclaration = typeof(ICrypticStructure).GetMethod("Serialize");
            typeBuilder.DefineMethodOverride(serializeBuilder, serializeDeclaration);
            serializeBuilder.DefineParameter(1, ParameterAttributes.None, "stream");

            var msil = serializeBuilder.GetILGenerator();

            var isNullLabel = msil.DefineLabel();
            var isNullLocal = msil.DeclareLocal(typeof(bool));

            msil.Emit(OpCodes.Ldarg_1);
            msil.Emit(OpCodes.Ldnull);
            msil.Emit(OpCodes.Ceq);
            msil.Emit(OpCodes.Ldc_I4_0);
            msil.Emit(OpCodes.Ceq);
            msil.Emit(OpCodes.Stloc, isNullLocal);
            msil.Emit(OpCodes.Ldloc, isNullLocal);
            msil.Emit(OpCodes.Brtrue_S, isNullLabel);

            msil.Emit(OpCodes.Ldstr, "stream");
            msil.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new Type[] { typeof(string) }));
            msil.Emit(OpCodes.Throw);

            msil.MarkLabel(isNullLabel);

            foreach (var column in table.Columns
                .Where(c => Helpers.IsGoodColumn(c)))
            {
                var token = Parser.GlobalTokens.GetToken(column.Token);
                var methodInfo = Helpers.GetSerializeMethod(column);

                var basicFlags = Parser.ColumnFlags.None;
                basicFlags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.EARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.INDIRECT;

                if (string.IsNullOrEmpty(column.StaticDefineListExternalName) == false ||
                    column.StaticDefineList != null)
                {
                    if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                    }
                    else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]), null);
                    }
                }
                else if (column.Token == 20) // structure
                {
                    if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                    }
                    else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column]), null);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo.MakeGenericMethod(fieldTypes[column].GetGenericArguments()[0]), null);
                    }
                }
                else if (column.Token == 21) // polymorph
                {
                    if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.Emit(basicFlags == Parser.ColumnFlags.INDIRECT ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                    else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.Emit(OpCodes.Ldsfld, polymorphBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                }
                else
                {
                    if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                    else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitConstant(column.NumberOfElements);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                    else
                    {
                        msil.Emit(OpCodes.Ldarg_1);
                        msil.Emit(OpCodes.Ldarg_0);
                        msil.Emit(OpCodes.Ldflda, fieldBuilders[column]);
                        msil.EmitCall(OpCodes.Callvirt, methodInfo, null);
                    }
                }
            }

            msil.Emit(OpCodes.Ret);
        }
    }
}
