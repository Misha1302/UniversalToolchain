using Example.Scenarios;

var verbose = Array.Exists(args, argument => argument == "--verbose");

PricingDiscountScenario.Run(verbose);