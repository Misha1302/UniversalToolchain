global using System.Collections;
global using System.Diagnostics.CodeAnalysis;
global using System.Text.RegularExpressions;
global using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;
global using BasicCore.Binding;
global using BasicCore.Binding.Symbols;
global using BasicCore.Compilation;
global using BasicCore.Contracts;
global using BasicCore.Execution;
global using BasicCore.ExecutorWrapper;
global using BasicCore.LexerWrapper;
global using BasicCore.ParserWrapper;
global using BasicCore.TranslatorWrapper;
global using BasicTypesExtensions;
global using DynamicMethodWrapper;
global using ExceptionsManager;
global using IntermediateRepresentationAbstractions;
global using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;
global using Microsoft.Extensions.DependencyInjection;
global using UniversalToolchain.Ir.Abstractions;

global using BasicCore.Semantics;

global using BasicCore.Builtins;

global using BasicCore.Legacy;
