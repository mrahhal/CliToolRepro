using Microsoft.DotNet.PlatformAbstractions;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CA
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			Console.WriteLine(RuntimeEnvironment.OperatingSystem);

			if (DotnetToolDispatcher.IsDispatcher(args))
			{
				Dispatch(args);
			}
			else
			{
				DotnetToolDispatcher.EnsureValidDispatchRecipient(ref args);
				// Do work
			}
		}

		private static void Dispatch(string[] args)
		{
			var projectFile = ProjectReader.GetProject(string.Empty);
			var tf = projectFile.TargetFrameworks.First();

			var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
				args,
				tf,
				"Debug",
				outputPath: null,
				buildBasePath: null,
				projectDirectory: projectFile.ProjectDirectory);

			using (var errorWriter = new StringWriter())
			{
				var commandExitCode = dispatchCommand
					.ForwardStdErr(errorWriter)
					.ForwardStdOut()
					.Execute()
					.ExitCode;

				if (commandExitCode != 0)
				{
					Console.WriteLine(errorWriter.ToString());
				}

				return;
			}
		}

		private static bool TryResolveFramework(
			IEnumerable<NuGetFramework> availableFrameworks,
			out NuGetFramework resolvedFramework)
		{
			NuGetFramework framework;
			framework = availableFrameworks.First();

			resolvedFramework = framework;
			return true;
		}
	}
}