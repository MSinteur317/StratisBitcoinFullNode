﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Stratis.SmartContracts.Core.Exceptions;

namespace Stratis.SmartContracts.Core.Util
{
    /// <summary>
    /// Helpful methods to handle compilation of .cs files.
    /// </summary>
    public static class GetFileDllHelper
    {
        /// <summary>
        /// Get the compiled bytecode for the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public static byte[] GetAssemblyBytesFromFile(string filename)
        {
            string source = File.ReadAllText(filename);
            return GetAssemblyBytesFromSource(source);
        }

        /// <summary>
        /// Get the compiled bytecode for the specified C# source code.
        /// </summary>
        /// <param name="source"></param>
        public static byte[] GetAssemblyBytesFromSource(string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

            CSharpCompilation compilation = CSharpCompilation.Create(
                "smartContract",
                new[] { syntaxTree },
                GetReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var dllStream = new MemoryStream())
            using (var pdbStream = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(dllStream, pdbStream);
                if (!emitResult.Success)
                    throw new Exception("Smart contract compilation failed.");

                return dllStream.ToArray();
            }
        }

        /// <summary>
        /// Gets all references needed for compilation. Ideally should use the same list as the contract validator.
        /// </summary>
        private static IList<MetadataReference> GetReferences()
        {
            var assemblyLocation = typeof(Enumerable).Assembly.Location;
            DirectoryInfo coreDir = Directory.GetParent(assemblyLocation);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(coreDir.FullName + Path.DirectorySeparatorChar + "mscorlib.dll"),
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            AssemblyName[] referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();

            foreach (AssemblyName referencedAssembly in referencedAssemblies)
            {
                var loadedAssembly = Assembly.Load(referencedAssembly);

                references.Add(MetadataReference.CreateFromFile(loadedAssembly.Location));
            }

            references.Add(MetadataReference.CreateFromFile(typeof(Address).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(SmartContract).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(SmartContractException).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));

            return references;
        }
    }
}