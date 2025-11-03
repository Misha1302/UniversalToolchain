const logData = {
    code: `a : RealNumberImpl = -5
@label:
    a = a + 1
    Main.Print(a)
goto @label`,

    lexemes: [
        { type: "Identifier", value: "a", position: "0:0" },
        { type: "Colon", value: ":", position: "2:2" },
        { type: "Identifier", value: "RealNumberImpl", position: "4:4" },
        { type: "Equality", value: "=", position: "19:19" },
        { type: "Number", value: "-5", position: "21:21" },
        { type: "Identifier", value: "@label", position: "24:0" },
        { type: "Colon", value: ":", position: "30:6" },
        { type: "Identifier", value: "a", position: "36:4" },
        { type: "Equality", value: "=", position: "38:6" },
        { type: "Identifier", value: "a", position: "40:8" },
        { type: "Addition", value: "+", position: "42:10" },
        { type: "Number", value: "1", position: "44:12" },
        { type: "Identifier", value: "Main.Print", position: "50:4" },
        { type: "OpenPar", value: "(", position: "60:14" },
        { type: "Identifier", value: "a", position: "61:15" },
        { type: "ClosePar", value: ")", position: "62:16" },
        { type: "Goto", value: "goto", position: "64:0" },
        { type: "Identifier", value: "@label", position: "69:5" }
    ],

    ast: `Scope:  : [
    Equality: = (Equality:"=") at 19:19 : [
        Variable: a (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 0:0 : [
            Colon: : (Colon:":") at 2:2 : [

            ]
            Identifier: RealNumberImpl (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 4:4 : [

            ]
        ]
        Number: -5 (Number:"(\\+|-)?([0-9]+)(\\.[0-9]+)?") at 21:21 : [

        ]
    ]
    Label: @label (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 24:0 : [
        Colon: : (Colon:":") at 30:6 : [

        ]
    ]
    Equality: = (Equality:"=") at 38:6 : [
        Variable: a (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 36:4 : [

        ]
        Addition: \\+ (Addition:"\\+") at 42:10 : [
            Variable: a (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 40:8 : [

            ]
            Number: 1 (Number:"(\\+|-)?([0-9]+)(\\.[0-9]+)?") at 44:12 : [

            ]
        ]
    ]
    CSharpFunctionCall: Main\\.Print (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 50:4 : [
        Scope:  : [
            Variable: a (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 61:15 : [

            ]
        ]
    ]
    Goto: goto (Goto:"goto") at 64:0 : [
        Identifier: @label (Identifier:"[@a-zA-Z_][a-zA-Z0-9_]*(?:\\.[a-zA-Z_][a-zA-Z0-9_]*)*(?:<[^<>]*(?:<(?:[^<>]|<<|>>)*>[^<>]*)*>)?") at 69:5 : [

        ]
    ]
]`,

    bytecode: [
        { address: "[]", op: "[0=LoadReferenceToLocalVar_a]" },
        { address: "[]", op: "[0=PushNumber_-5]" },
        { address: "[]", op: "[0=Set_a=-5]" },
        { address: "[]", op: "[0=Label_!Intrinsic_@label]" },
        { address: "[]", op: "[0=LoadReferenceToLocalVar_a]" },
        { address: "[]", op: "[0=LoadValueOfLocalVar_a]" },
        { address: "[]", op: "[0=PushNumber_1]" },
        { address: "[]", op: "[0=Op_+]" },
        { address: "[]", op: "[0=Set_a=+]" },
        { address: "[]", op: "[0=LoadValueOfLocalVar_a]" },
        { address: "[]", op: "[0=Call_Main.Print]" },
        { address: "[]", op: "[0=Goto_!Intrinsic_@label]" }
    ],

    dotnet: `LoadReferenceToLocalVar_a:
        ldstr 'a'                                         // [String]
        call VariableReference<RealNumberImpl> VariablesContainer<RealNumberImpl>.GetRef(String)
                                                          // [VariableReference<RealNumberImpl>]
        ret                                               // []


PushNumber_-5:
        ldc.r8 -5                                 // [Double]
        newobj RealNumberImpl.ctor(Double)        // [RealNumberImpl]
        ret                                       // []


Set_a=-5:
        ldarg.0                                           // [VariableReference<RealNumberImpl>]
        ldarg.1                                           // [VariableReference<RealNumberImpl>, RealNumberImpl]
        callvirt Void VariableReference<RealNumberImpl>.SetValue(RealNumberImpl)
                                                          // []
        ret                                               // []


Label_!Intrinsic_@label:
        ldstr 'Intrinsic function was not overloaded'     // [String]
        newobj NotImplementedException.ctor(String)       // [NotImplementedException]
        throw                                             // []


LoadReferenceToLocalVar_a:
        ldstr 'a'                                         // [String]
        call VariableReference<RealNumberImpl> VariablesContainer<RealNumberImpl>.GetRef(String)
                                                          // [VariableReference<RealNumberImpl>]
        ret                                               // []


LoadValueOfLocalVar_a:
        ldstr 'a'                                         // [String]
        call RealNumberImpl VariablesContainer<RealNumberImpl>.Get(String)
                                                          // [RealNumberImpl]
        ret                                               // []


PushNumber_1:
        ldc.r8 1                                  // [Double]
        newobj RealNumberImpl.ctor(Double)        // [RealNumberImpl]
        ret                                       // []


Op_+:
        ldarg.0                                           // [RealNumberImpl]
        ldarg.1                                           // [RealNumberImpl, RealNumberImpl]
        call RealNumberImpl RealNumberImpl.Add(RealNumberImpl, RealNumberImpl)
                                                          // [RealNumberImpl]
        ret                                               // []


Set_a=+:
        ldarg.0                                           // [VariableReference<RealNumberImpl>]
        ldarg.1                                           // [VariableReference<RealNumberImpl>, RealNumberImpl]
        callvirt Void VariableReference<RealNumberImpl>.SetValue(RealNumberImpl)
                                                          // []
        ret                                               // []


LoadValueOfLocalVar_a:
        ldstr 'a'                                         // [String]
        call RealNumberImpl VariablesContainer<RealNumberImpl>.Get(String)
                                                          // [RealNumberImpl]
        ret                                               // []


Call_Main.Print:
        ldarg.0                             // [RealNumberImpl]
        box RealNumberImpl                  // [Object]
        castclass Object                    // [Object]
        call Void Main.Print(Object)        // []
        ret                                 // []


Goto_!Intrinsic_@label:
        ldstr 'Intrinsic function was not overloaded'     // [String]
        newobj NotImplementedException.ctor(String)       // [NotImplementedException]
        throw                                             // []`
};
