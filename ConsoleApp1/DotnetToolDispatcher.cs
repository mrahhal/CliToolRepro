﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.DotNet.Cli.Utils;
using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CA
{
	internal static class DotnetToolDispatcher
	{
		private const string DispatcherVersionArgumentName = "--dispatcher-version";

		private static readonly string DispatcherName = "dotnet-some";

		public static ICommand CreateDispatchCommand(
			IEnumerable<string> dispatchArgs,
			NuGetFramework framework,
			string configuration,
			string outputPath,
			string buildBasePath,
			string projectDirectory) =>
				CreateDispatchCommand(
					dispatchArgs,
					framework,
					configuration,
					outputPath,
					buildBasePath,
					projectDirectory,
					DispatcherName);

		public static ICommand CreateDispatchCommand(
			IEnumerable<string> dispatchArgs,
			NuGetFramework framework,
			string configuration,
			string outputPath,
			string buildBasePath,
			string projectDirectory,
			string toolName)
		{
			if (buildBasePath != null && !Path.IsPathRooted(buildBasePath))
			{
				// ProjectDependenciesCommandFactory cannot handle relative build base paths.
				buildBasePath = Path.Combine(Directory.GetCurrentDirectory(), buildBasePath);
			}

			configuration = configuration ?? Constants.DefaultConfiguration;
			var commandFactory = new ProjectDependenciesCommandFactory(
				framework,
				configuration,
				outputPath,
				buildBasePath,
				projectDirectory);

			var dispatcherVersionArgumentValue = ResolveDispatcherVersionArgumentValue(DispatcherName);
			var dispatchArgsList = new List<string>(dispatchArgs)
			{
				DispatcherVersionArgumentName,
				dispatcherVersionArgumentValue
			};
			return commandFactory.Create(toolName, dispatchArgsList, framework, configuration);
		}

		public static bool IsDispatcher(string[] programArgs) =>
			!programArgs.Contains(DispatcherVersionArgumentName, StringComparer.OrdinalIgnoreCase);

		public static void EnsureValidDispatchRecipient(ref string[] programArgs) =>
			EnsureValidDispatchRecipient(ref programArgs, DispatcherName);

		public static void EnsureValidDispatchRecipient(ref string[] programArgs, string toolName)
		{
			if (!programArgs.Contains(DispatcherVersionArgumentName, StringComparer.OrdinalIgnoreCase))
			{
				return;
			}

			var dispatcherArgumentIndex = Array.FindIndex(
				programArgs,
				(value) => string.Equals(value, DispatcherVersionArgumentName, StringComparison.OrdinalIgnoreCase));
			var dispatcherArgumentValueIndex = dispatcherArgumentIndex + 1;
			if (dispatcherArgumentValueIndex < programArgs.Length)
			{
				var dispatcherVersion = programArgs[dispatcherArgumentValueIndex];

				var dispatcherVersionArgumentValue = ResolveDispatcherVersionArgumentValue(toolName);
				if (string.Equals(dispatcherVersion, dispatcherVersionArgumentValue, StringComparison.Ordinal))
				{
					// Remove dispatcher arguments from
					var preDispatcherArgument = programArgs.Take(dispatcherArgumentIndex);
					var postDispatcherArgument = programArgs.Skip(dispatcherArgumentIndex + 2);
					var newProgramArguments = preDispatcherArgument.Concat(postDispatcherArgument);
					programArgs = newProgramArguments.ToArray();
					return;
				}
			}

			// Could not validate the dispatchers version.
			throw new InvalidOperationException(
				$"Could not invoke tool {toolName}. Ensure it has matching versions in the project.json's 'dependencies' and 'tools' sections.");
		}

		// Internal for testing
		internal static string ResolveDispatcherVersionArgumentValue(string toolName)
		{
			var toolAssembly = Assembly.Load(new AssemblyName(toolName));

			var informationalVersionAttribute = toolAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

			Debug.Assert(informationalVersionAttribute != null);

			var informationalVersion = informationalVersionAttribute?.InformationalVersion ??
				toolAssembly.GetName().Version.ToString();

			return informationalVersion;
		}
	}
}
