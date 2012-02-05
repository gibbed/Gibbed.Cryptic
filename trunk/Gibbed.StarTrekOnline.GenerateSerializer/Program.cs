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
using NDesk.Options;
using Parser = Gibbed.Cryptic.FileFormats.Parser;
using ParserSchema = Gibbed.Cryptic.FileFormats.ParserSchema;

namespace Gibbed.StarTrekOnline.GenerateSerializer
{
    internal class Program
    {
        private static string GetExecutablePath()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private static string GetColumnName(ParserSchema.Column column)
        {
            if (string.IsNullOrEmpty(column.Name) == true)
            {
                return "_";
            }

            return column.Name;
        }

        private static Type GetColumnNativeType(ParserSchema.Column column, Dictionary<ParserSchema.Table, TypeBuilder> tableTypes)
        {
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
                    case 20: return tableTypes[column.Subtable];
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
                    case 20: return typeof(List<>).MakeGenericType(tableTypes[column.Subtable]);
                    case 21: return typeof(List<object>);
                    case 24: return typeof(List<MultiValue>);
                    default: throw new NotImplementedException();
                }
            }
        }

        private static bool IsGoodColumn(ParserSchema.Column column)
        {
            if ((column.Flags & Parser.ColumnFlags.ALIAS) != 0 ||
                (column.Flags & Parser.ColumnFlags.UNKNOWN_32) != 0 ||
                (column.Flags & Parser.ColumnFlags.NO_WRITE) != 0)
            {
                return false;
            }
            else if (column.Token == 0 || // ignore
                column.Token == 1 || // start
                column.Token == 2 || // end
                column.Token == 25) // command
            {
                return false;
            }

            return true;
        }

        private static MethodInfo GetSerializeMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);

            string name = null;

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                name = "SerializeValue" + token.GetType().Name;
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                name = "SerializeArray" + token.GetType().Name;
            }
            else
            {
                name = "SerializeList" + token.GetType().Name;
            }

            var methodInfo = typeof(ICrypticStream).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new NotSupportedException(name + " is missing");
            }

            return methodInfo;
        }

        private static void Generate(ParserSchema.Table table, TypeBuilder typeBuilder, Dictionary<ParserSchema.Table, TypeBuilder> tableTypes)
        {
            var fieldBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var fieldTypes = new Dictionary<ParserSchema.Column, Type>();

            foreach (var column in table.Columns
                .Where(c => IsGoodColumn(c)))
            {
                var fieldType = GetColumnNativeType(column, tableTypes);
                fieldTypes.Add(column, fieldType);

                var fieldBuilder = typeBuilder.DefineField(
                    GetColumnName(column),
                    fieldType,
                    FieldAttributes.Public);

                fieldBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataMemberAttribute)
                        .GetConstructor(Type.EmptyTypes), new object[] { }));

                fieldBuilders.Add(column, fieldBuilder);
            }

            var ctorBuilder = typeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

            var polymorphAttributeTypes = new List<Type>();

            var polymorphBuilders = new Dictionary<ParserSchema.Column, FieldBuilder>();
            var polymorphColumns = table.Columns.Where(c => IsGoodColumn(c) && c.Token == 21);
            if (polymorphColumns.Count() > 0)
            {
                var cctorBuilder = typeBuilder.DefineTypeInitializer();
                var cctorMsil = cctorBuilder.GetILGenerator();
                var typeList = cctorMsil.DeclareLocal(typeof(Type[]));

                foreach (var column in polymorphColumns)
                {
                    var fieldBuilder = typeBuilder.DefineField(
                        "_ValidFor" + GetColumnName(column),
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
                        
                        cctorMsil.Emit(OpCodes.Ldtoken, GetColumnNativeType(subcolumn, tableTypes));
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
                        var type = GetColumnNativeType(subcolumn, tableTypes);
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

            var label = msil.DefineLabel();
            var isNull = msil.DeclareLocal(typeof(bool));

            msil.Emit(OpCodes.Ldarg_1);
            msil.Emit(OpCodes.Ldnull);
            msil.Emit(OpCodes.Ceq);
            msil.Emit(OpCodes.Ldc_I4_0);
            msil.Emit(OpCodes.Ceq);
            msil.Emit(OpCodes.Stloc, isNull);
            msil.Emit(OpCodes.Ldloc, isNull);
            msil.Emit(OpCodes.Brtrue_S, label);

            msil.Emit(OpCodes.Ldstr, "stream");
            msil.Emit(OpCodes.Newobj, typeof(ArgumentNullException).GetConstructor(new Type[] { typeof(string) }));
            msil.Emit(OpCodes.Throw);

            msil.MarkLabel(label);

            foreach (var column in table.Columns
                .Where(c => IsGoodColumn(c)))
            {
                var token = Parser.GlobalTokens.GetToken(column.Token);
                var methodInfo = GetSerializeMethod(column);

                var basicFlags = Parser.ColumnFlags.None;
                basicFlags |= column.Flags & Parser.ColumnFlags.FIXED_ARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.EARRAY;
                basicFlags |= column.Flags & Parser.ColumnFlags.INDIRECT;

                if (column.Token == 20) // structure
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

        public static void Main(string[] args)
        {
            var showHelp = false;
            string version = null;

            var options = new OptionSet()
            {
                {
                    "v|version=",
                    "set version",
                    v => version = v
                },
                {
                    "h|help",
                    "show this message and exit", 
                    v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 0 || extras.Count > 1 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ [output_dll]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var loader = new ParserLoader(
                Path.Combine(GetExecutablePath(), "parsers", "Star Trek Online"));

            var outputPath = extras.Count > 0 ? extras[0] : "Gibbed.StarTrekOnline.Serialization.dll";

            var assemblyName = new AssemblyName();
            assemblyName.Name = Path.GetFileNameWithoutExtension(outputPath);

            var currentDomain = AppDomain.CurrentDomain;

            var assemblyBuilder = currentDomain.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Save);

            if (version != null)
            {
                assemblyBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(AssemblyDescriptionAttribute)
                        .GetConstructor(new Type[] { typeof(string) }), new object[] { version }));
            }

            assemblyBuilder.DefineVersionInfoResource();

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(
                assemblyName.Name, Path.GetFileName(outputPath));

            var queue = new Queue<QueuedType>();
            var done = new List<string>();

            var tableTypes = new Dictionary<ParserSchema.Table, TypeBuilder>();

            queue.Clear();
            done.Clear();
            foreach (var parserName in loader.ParserNames)
            {
                queue.Enqueue(new QueuedType(parserName, loader.LoadParser(parserName).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                TypeBuilder typeBuilder;

                if (qt.Parent == null)
                {
                    typeBuilder = moduleBuilder.DefineType(
                        "Gibbed.StarTrekOnline.Serialization." + qt.Name,
                        TypeAttributes.Public,
                        null,
                        new Type[] { typeof(ICrypticStructure) });
                }
                else
                {
                    typeBuilder = tableTypes[qt.Parent.Table].DefineNestedType(
                        qt.Name,
                        TypeAttributes.NestedPublic,
                        null,
                        new Type[] { typeof(ICrypticStructure) });
                }

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataContractAttribute)
                        .GetConstructor(Type.EmptyTypes), new object[] { }));

                tableTypes.Add(qt.Table, typeBuilder);

                foreach (var column in qt.Table.Columns
                    .Where(c => IsGoodColumn(c)))
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
            foreach (var parserName in loader.ParserNames)
            {
                queue.Enqueue(new QueuedType(parserName, loader.LoadParser(parserName).Table));
            }

            while (queue.Count > 0)
            {
                var qt = queue.Dequeue();
                done.Add(qt.Key);

                var typeBuilder = tableTypes[qt.Table];

                Generate(qt.Table, typeBuilder, tableTypes);

                foreach (var column in qt.Table.Columns
                    .Where(c => IsGoodColumn(c)))
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

            assemblyBuilder.Save(outputPath);
        }
    }
}
