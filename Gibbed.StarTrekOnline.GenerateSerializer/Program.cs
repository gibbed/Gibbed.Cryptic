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
                    case 7: return typeof(float);
                    case 8: return typeof(string);
                    case 9: return typeof(string);
                    case 18: return typeof(string);
                    case 20: return tableTypes[column.Subtable];
                    case 21: return typeof(object);
                    case 23: return typeof(uint);
                    default: throw new NotImplementedException();
                }
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                switch (column.Token)
                {
                    case 4: return typeof(short[]);
                    case 5: return typeof(int[]);
                    case 7: return typeof(float[]);
                    default: throw new NotImplementedException();
                }
            }
            else
            {
                switch (column.Token)
                {
                    case 5: return typeof(List<int>);
                    case 8: return typeof(List<string>);
                    case 20: return typeof(List<>).MakeGenericType(tableTypes[column.Subtable]);
                    case 24: return typeof(List<MultiValueInstruction>);
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
            else if (column.Token == 0 ||
                column.Token == 1 ||
                column.Token == 2)
            {
                return false;
            }

            return true;
        }

        private static MethodInfo GetSerializeMethod(ParserSchema.Column column)
        {
            var token = Parser.GlobalTokens.GetToken(column.Token);
            string methodName = null;

            if ((column.Flags & (Parser.ColumnFlags.EARRAY | Parser.ColumnFlags.FIXED_ARRAY)) == 0)
            {
                methodName = "SerializeValue" + token.GetType().Name;
            }
            else if ((column.Flags & Parser.ColumnFlags.EARRAY) == 0)
            {
                methodName = "SerializeArray" + token.GetType().Name;
            }
            else
            {
                methodName = "SerializeList" + token.GetType().Name;
            }

            var methodInfo = typeof(ICrypticStream).GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new NotSupportedException(methodName + " is missing");
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
            isNull.SetLocalSymInfo("bob");

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

            var options = new OptionSet()
            {
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

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ target_name [output_dll]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var loader = new ParserLoader(
                Path.Combine(GetExecutablePath(), "parsers", "Star Trek Online"));

            var inputName = extras[0];
            var outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputName, ".dll");

            var parser = loader.LoadParser(inputName);

            var assemblyName = new AssemblyName();
            assemblyName.Name = Path.GetFileNameWithoutExtension(outputPath);

            var currentDomain = AppDomain.CurrentDomain;

            var assemblyBuilder = currentDomain.DefineDynamicAssembly(
                assemblyName,
                AssemblyBuilderAccess.Save);
            

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(
                assemblyName.Name, assemblyName.Name + ".dll", true);

            var queue = new Queue<KeyValuePair<string, ParserSchema.Table>>();
            var done = new List<string>();

            var tableTypes = new Dictionary<ParserSchema.Table, TypeBuilder>();

            queue.Clear();
            done.Clear();
            queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(inputName, parser.Table));
            while (queue.Count > 0)
            {
                var kv = queue.Dequeue();
                var parserName = kv.Key;
                var parserTable = kv.Value;

                done.Add(parserName);

                var typeBuilder = moduleBuilder.DefineType(
                    assemblyName.Name + "." + parserName,
                    TypeAttributes.Public,
                    null,
                    new Type[] { typeof(ICrypticStructure) });

                typeBuilder.SetCustomAttribute(
                    new CustomAttributeBuilder(typeof(DataContractAttribute)
                        .GetConstructor(Type.EmptyTypes), new object[] { }));

                tableTypes.Add(parserTable, typeBuilder);

                foreach (var column in parserTable.Columns
                    .Where(c => IsGoodColumn(c)))
                {
                    if (column.Subtable != null &&
                        string.IsNullOrEmpty(column.SubtableExternalName) == false &&
                        done.Contains(column.SubtableExternalName) == false &&
                        queue.Where(e => e.Key == column.SubtableExternalName).Count() == 0)
                    {
                        queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(column.SubtableExternalName, column.Subtable));
                    }

                    if (column.Token == 21)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.Subtable != null &&
                                string.IsNullOrEmpty(subcolumn.SubtableExternalName) == false &&
                                done.Contains(subcolumn.SubtableExternalName) == false &&
                                queue.Where(e => e.Key == subcolumn.SubtableExternalName).Count() == 0)
                            {
                                queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(subcolumn.SubtableExternalName, subcolumn.Subtable));
                            }
                        }
                    }
                }
            }

            queue.Clear();
            done.Clear();
            queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(inputName, parser.Table));
            while (queue.Count > 0)
            {
                var kv = queue.Dequeue();
                var parserName = kv.Key;
                var parserTable = kv.Value;

                done.Add(parserName);

                var typeBuilder = tableTypes[parserTable];

                Generate(parserTable, typeBuilder, tableTypes);

                foreach (var column in parserTable.Columns
                    .Where(c => IsGoodColumn(c)))
                {
                    if (column.Subtable != null &&
                        string.IsNullOrEmpty(column.SubtableExternalName) == false &&
                        done.Contains(column.SubtableExternalName) == false &&
                        queue.Where(e => e.Key == column.SubtableExternalName).Count() == 0)
                    {
                        queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(column.SubtableExternalName, column.Subtable));
                    }

                    if (column.Token == 21)
                    {
                        foreach (var subcolumn in column.Subtable.Columns)
                        {
                            if (subcolumn.Subtable != null &&
                                string.IsNullOrEmpty(subcolumn.SubtableExternalName) == false &&
                                done.Contains(subcolumn.SubtableExternalName) == false &&
                                queue.Where(e => e.Key == subcolumn.SubtableExternalName).Count() == 0)
                            {
                                queue.Enqueue(new KeyValuePair<string, ParserSchema.Table>(subcolumn.SubtableExternalName, subcolumn.Subtable));
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
