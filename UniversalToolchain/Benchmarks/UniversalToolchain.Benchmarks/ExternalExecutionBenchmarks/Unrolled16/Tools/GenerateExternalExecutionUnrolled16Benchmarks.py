#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

HEADER = """using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Order;
using DynamicExpresso;
using DynamicMethodCalling.Core;
using NCalc;
using NCalc.LambdaCompilation;

namespace UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks.Unrolled16;

[MemoryDiagnoser]
[SimpleJob]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
"""

CASES = [
    {
        "class": "ExternalSimple3ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext3Unrolled16",
        "arity": 3,
        "wist": "A + B * C / 5.0",
        "ncalc": "[A] + [B] * [C] / 5.0",
        "dynamic": "A + B * C / 5.0",
        "core": "a + b * c / 5.0",
    },
    {
        "class": "ExternalMedium8ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext8Unrolled16",
        "arity": 8,
        "wist": "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0",
        "ncalc": "(([A] + [B]) * ([C] - [D]) / ([E] + 1.0)) + [F] * [G] - [H] / 3.0",
        "dynamic": "((A + B) * (C - D) / (E + 1.0)) + F * G - H / 3.0",
        "core": "((a + b) * (c - d) / (e + 1.0)) + f * g - h / 3.0",
    },
    {
        "class": "ExternalDeepChain6ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext6Unrolled16",
        "arity": 6,
        "wist": "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)",
        "ncalc": "(((([A] * 1.1 + [B]) * 1.2 + [C]) * 1.3 + [D]) * 1.4 + [E]) / ([F] + 1.0)",
        "dynamic": "((((A * 1.1 + B) * 1.2 + C) * 1.3 + D) * 1.4 + E) / (F + 1.0)",
        "core": "((((a * 1.1 + b) * 1.2 + c) * 1.3 + d) * 1.4 + e) / (f + 1.0)",
    },
    {
        "class": "ExternalRepeatedSubexpressions5ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext5Unrolled16",
        "arity": 5,
        "wist": "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)",
        "ncalc": "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + ([C] * [D])) / ([E] + 1.0)",
        "dynamic": "((A * B) + (A * B) + (A * B) + (C * D)) / (E + 1.0)",
        "core": "((a * b) + (a * b) + (a * b) + (c * d)) / (e + 1.0)",
    },
    {
        "class": "ExternalWideExpression11ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext11Unrolled16",
        "arity": 11,
        "wist": "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0",
        "ncalc": "([A] + [B] + [C] + [D]) * ([E] - [F] + [G]) / ([H] + 1.0) + [I] * [J] - [K] / 3.0",
        "dynamic": "(A + B + C + D) * (E - F + G) / (H + 1.0) + I * J - K / 3.0",
        "core": "(a + b + c + d) * (e - f + g) / (h + 1.0) + i * j - k / 3.0",
    },
    {
        "class": "ExternalConstantsHeavy6ExecutionUnrolled16Benchmarks",
        "context": "ExternalBenchContext6Unrolled16",
        "arity": 6,
        "wist": "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F",
        "ncalc": "([A] * 1.5 + [B] * 2.0 - [C] * 3.0 + [D] / 4.0 + [E] / 5.0) * 0.75 + [F]",
        "dynamic": "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F",
        "core": "(a * 1.5 + b * 2.0 - c * 3.0 + d / 4.0 + e / 5.0) * 0.75 + f",
    },
]


def letters(arity: int):
    return [chr(ord('A') + i) for i in range(arity)]


def csharp_params(arity: int):
    return ", ".join(f"double {chr(ord('a')+i)}" for i in range(arity))


def generic_func(arity: int):
    return "Func<" + ", ".join(["double"] * (arity + 1)) + ">"


def dynamic_invoker(arity: int):
    return "DynamicMethodInvoker<" + ", ".join(["double"] * (arity + 1)) + ">"


def arg_access(names, idx='i'):
    return ", ".join(f"{n}[{idx}]" for n in names)


def declared_names(names):
    return ", ".join(f'"{n}"' for n in names)


def context_setters(context: str, names, idx='i'):
    return "\n".join(f"            {context}.{n} = {n}[{idx}];" for n in names)


def unrolled_lines(kind: str, names, context_name='_nCalcContext'):
    lines = []
    for i in range(16):
        lines.append(f"        var i{i} = NextIndex();")
        if kind == 'csharp':
            args = arg_access(names, f"i{i}")
            lines.append(f"        sum += CSharp_NoInliningMethodCore({args});")
        elif kind == 'dynamic':
            args = arg_access(names, f"i{i}")
            lines.append(f"        sum += _dynamicExpressoDelegate({args});")
        elif kind == 'wist':
            args = arg_access(names, f"i{i}")
            lines.append(f"        sum += _wistFastInvoker.Invoke({args});")
        else:
            for n in names:
                lines.append(f"        {context_name}.{n} = {n}[i{i}];")
            lines.append(f"        sum += _nCalcLambda({context_name});")
        lines.append("")
    return "\n".join(lines).rstrip()


def render(case):
    names = letters(case['arity'])
    class_name = case['class']
    context = case['context']
    src = f"""{HEADER}public sealed class {class_name} : ExternalArithmeticExecutionUnrolled16BenchmarkEnvironmentBase
{{
    private const string WistFormula = \"{case['wist']}\";
    private const string NCalcFormula = \"{case['ncalc']}\";
    private const string DynamicExpressoFormula = \"{case['dynamic']}\";

    private {context} _nCalcContext = null!;
    private Func<{context}, double> _nCalcLambda = null!;
    private {generic_func(case['arity'])} _dynamicExpressoDelegate = null!;
    private {dynamic_invoker(case['arity'])} _wistFastInvoker = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {{
        InitializeInputData();
        CreateProviderAndHost();

        var dynamicMethod = CompileWistDynamicMethod(WistFormula, [{declared_names(names)}]);
        _wistFastInvoker = new {dynamic_invoker(case['arity'])}(dynamicMethod);

        var nCalcExpression = new Expression(NCalcFormula);
        _nCalcLambda = nCalcExpression.ToLambda<{context}, double>();
        _nCalcContext = new {context}();

        var dynamicExpressoInterpreter = new Interpreter();
        _dynamicExpressoDelegate =
            dynamicExpressoInterpreter.ParseAsDelegate<{generic_func(case['arity'])}>(
                DynamicExpressoFormula,
                {declared_names(names)});

        EnsureResultParityAcrossIndexes(CSharpAt, DynamicExpressoAt, NCalcAt, WistAt);
    }}

    [Benchmark(Baseline = true, OperationsPerInvoke = 16)]
    public double CSharp_NoInliningMethod_Unrolled16()
    {{
        var sum = 0.0;

{unrolled_lines('csharp', names)}

        return sum;
    }}

    [Benchmark(OperationsPerInvoke = 16)]
    public double DynamicExpresso_Delegate_Unrolled16()
    {{
        var sum = 0.0;

{unrolled_lines('dynamic', names)}

        return sum;
    }}

    [Benchmark(OperationsPerInvoke = 16)]
    public double NCalc_Lambda_Unrolled16()
    {{
        var sum = 0.0;

{unrolled_lines('ncalc', names)}

        return sum;
    }}

    [Benchmark(OperationsPerInvoke = 16)]
    public double Wist_Cil_FastInvoker_Unrolled16()
    {{
        var sum = 0.0;

{unrolled_lines('wist', names)}

        return sum;
    }}

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double CSharp_NoInliningMethodCore({csharp_params(case['arity'])})
        => {case['core']};

    private double CSharpAt(int index)
        => CSharp_NoInliningMethodCore({arg_access(names, 'index')});

    private double DynamicExpressoAt(int index)
        => _dynamicExpressoDelegate({arg_access(names, 'index')});

    private double NCalcAt(int index)
    {{
{context_setters('_nCalcContext', names, 'index')}
        return _nCalcLambda(_nCalcContext);
    }}

    private double WistAt(int index)
        => _wistFastInvoker.Invoke({arg_access(names, 'index')});
}}
"""
    return src


for case in CASES:
    path = ROOT / f"{case['class']}.cs"
    path.write_text(render(case), encoding='utf-8')
    print(f"generated {path}")
