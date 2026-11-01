using System;

namespace SCLauncher.Backend.Install;

internal class UnsetInstallPathException() : Exception("Install path has not been set");