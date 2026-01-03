The following outlines the desired folder structure of the project
```
/src
  /Middagsklok                (main – application logic)
    /Features
    /Domain
    /Database
    /Gateways
  /Middagsklok.App            (Console app, primary usage before API and UI is developed)
  /Middagsklok.Api            (HTTP entrypoint - Not currentlyt present)
  /Middagsklok.Contracts      (DTOer/contracts which the API exposes, optional)
  /Middagsklok.Migrations     (optional – to isolate migrations)
/tests
  /Middagsklok.Tests.Unit
  /Middagsklok.Tests.Integration
/docs
    architecture.md
    folderstructure.md
Middagsklok.sln
README.md
```