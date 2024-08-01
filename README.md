## Introduction ##
This is Firely's official FHIR validator API for validating [HL7 FHIR][fhir-spec] resources against [profiles][profiles].
These profiles contain the full gamut of FHIR validation rules, and are used to validate the data in the FHIR resources.

## Release notes ##
Read the releases notes on [firely-net-sdk/releases](https://github.com/FirelyTeam/firely-validator-api/releases).

## Documentation ##
You can find documentation on the validation api at [the Firely docs site][validator-docu].

## Getting Started ##
Before installing one of the NuGet packages (or clone the repo) it is important to understand that HL7 has published several updates of the FHIR specification, each with breaking changes - so you need to ensure you use the version that is right for you.
Read the [online documentation][validator-docu], and download the correct package for your FHIR release by searching for ``Firely.Fhir.Validation.<spec version>``. For most developers, just including this NuGet package is enough to get started. 

The main class in this package is the `Validator`.

An example implementation can be found [here][validator-demo].

### Using a pre-release NuGet package
Every release of the validator API results in a NuGet package on the normal NuGet feed. However, each commit on our develop branch also results in a pre-release package.
These are public too. So if you want to be brave and use a pre-release packages, you can do so by adding ```https://nuget.pkg.github.com/FirelyTeam/index.json``` to your NuGet sources:

- Get a Personal Access token (PAT) from [github.com][github-pat] with scope ```read:packages```

- Next open a console on your machine and run ```dotnet nuget add source --name github --username <USERNAME> --password <PAT> https://nuget.pkg.github.com/FirelyTeam/index.json```

```USERNAME```: your username on GitHub
```PAT```: your Personal access token with at least the scope ```read:packages```

## Support 
We actively monitor the issues coming in through the [GitHub repository][issues]. You are welcome to register your bugs and feature suggestions there. For questions and broader discussions, we use the .NET FHIR Implementers chat on [Zulip][netsdk-zulip].

## Contributing ##
We are welcoming contributions!

If you want to participate in this project, we're using [Git Flow][nvie] for our branch management. Please submit PRs on [GitHub][github] with changes against the `develop` branch.


[validator-docu]: https://docs.fire.ly/projects/Firely-NET-SDK/en/latest/validation/profile-validation.html#
[validator-demo]: https://github.com/FirelyTeam/Firely.Fhir.ValidationDemo
[netsdk-zulip]: https://chat.fhir.org/#narrow/stream/dotnet
[nvie]: http://nvie.com/posts/a-successful-git-branching-model/
[fhir-spec]: http://www.hl7.org/fhir
[profiles]: https://hl7.org/FHIR/profiling.html
[github-pat]: https://github.com/settings/apps
[github]: https://github.com/FirelyTeam/firely-validator-api
[issues]: https://github.com/FirelyTeam/firely-validator-api/issues
