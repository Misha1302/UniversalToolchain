using Example.Scenarios;

var verbose = Array.Exists(args, argument => argument == "--verbose");

ShowcaseRuleScenario.Run(verbose);
