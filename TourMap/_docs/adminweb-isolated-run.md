# AdminWeb Isolated Run Workflow

Use this when you want `TourMap.AdminWeb` to keep running without locking the normal `TourMap.AdminWeb\bin\Debug\net10.0` build output.

## Start the backend from an isolated run folder

```powershell
.\scripts\Start-AdminWebIsolated.ps1
```

That script:

- builds with a separate `.run\TourMap.AdminWeb\build` output tree
- publishes `TourMap.AdminWeb` into `.run\TourMap.AdminWeb\releases\<timestamp>`
- launches the published app from that release folder
- keeps the database and Data Protection keys pointed at the original `TourMap.AdminWeb` project directory

## Stop the isolated backend

```powershell
.\scripts\Stop-AdminWebIsolated.ps1
```

## Launch from Visual Studio

Select the `isolated-http` launch profile for `TourMap.AdminWeb` and start debugging normally.

That profile runs:

```powershell
.\scripts\Run-AdminWebIsolated.ps1
```

Unlike the detached start script, this Visual Studio launcher keeps the isolated backend in the foreground, so stopping the debug session stops that backend too.

## Normal dev loop

1. Start the isolated backend once.
2. Rebuild the solution as often as you want in Visual Studio or with `dotnet build`.
3. When you want the backend to pick up your latest code, stop it and start it again.

## Notes

- If an old backend is still running from `TourMap.AdminWeb\bin\Debug\net10.0`, stop it once before switching to this workflow.
- The running backend is a published snapshot. Rebuilding the solution does not update that running copy until you restart it.
- The current run metadata is saved to `.run\TourMap.AdminWeb\current-run.json`.
- The script writes launcher output to `console.log` inside the active release folder.
