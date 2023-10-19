using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace PublicizeAssembly
{
    public class PublicizeAssembly : Task
    {
        [Required]
        public string InputFilePath { get; set; }

        [Required]
        public string OutputFileName { get; set; }

        [Required]
        public string OutputFolderName { get; set; }

        [Output]
        public ITaskItem OutputFile { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(InputFilePath) || string.IsNullOrEmpty(OutputFileName) || string.IsNullOrEmpty(OutputFolderName)) return false;

            AssemblyDefinition assembly;

            if (!File.Exists(InputFilePath)) return false;

            try
            {
                assembly = AssemblyDefinition.ReadAssembly(InputFilePath);

            } catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }

            if (assembly == null) return false;

            IEnumerable<TypeDefinition> types = GetAllTypes(assembly.MainModule);
            IEnumerable<MethodDefinition> methods = types.SelectMany(t => t.Methods);
            IEnumerable<FieldDefinition> fields = types.SelectMany(t => t.Fields);

            foreach (TypeDefinition type in types)
            {
                if ((!type?.IsPublic ?? false) && !type.IsNestedPublic)
                {
                    if (type.IsNested)
                    {
                        type.IsNestedPublic = true;
                        continue;
                    }

                    type.IsPublic = true;
                }
            }

            foreach (MethodDefinition method in methods)
            {
                if (method?.IsPublic ?? true) continue;
                method.IsPublic = true;
            }

            foreach (FieldDefinition field in fields)
            {
                if (field?.IsPublic ?? true) continue;
                field.IsPublic = true;
            }

            string path = Path.Combine(OutputFolderName, OutputFileName);

            try
            {
                if (!Directory.Exists(OutputFolderName)) Directory.CreateDirectory(OutputFolderName);
                assembly.Write(path);
                OutputFile = new TaskItem(path);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, showStackTrace: true);
                return false;
            }
        }

        private IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition mainModule)
        {
            return GetAllNestTypes(mainModule.Types);
        }

        private IEnumerable<TypeDefinition> GetAllNestTypes(IEnumerable<TypeDefinition> types)
        {
            if (types?.Count() == 0) return new List<TypeDefinition>();

            return types.Concat(GetAllNestTypes(types.SelectMany(t => t.NestedTypes)));
        }
    }
}
