﻿// Copyright 2020-2022 Aumoa.lib. All right reserved.

using DotNETUtilities;

using System.Text;

namespace BuildTool;

class GeneratedCMakeSolution : IGeneratedSolution
{
    public readonly Solution CompiledSolution;

    public GeneratedCMakeSolution(Solution CompiledSolution)
    {
        this.CompiledSolution = CompiledSolution;
    }

    readonly Dictionary<string, string> ApplicationMacros = new();
    Dictionary<string, GeneratedCMakeProject> GeneratedProjects;
    List<string> SortedProjects;

    public void Generate(CMakeGenerator SlnGenerator)
    {
        GeneratedProjects = SlnGenerator.GeneratedProjects;
        SortedProjects = SlnGenerator.SortedProjects;

        string PlatformId = Environment.OSVersion.Platform switch
        {
            PlatformID.Unix => $"PLATFORM_LINUX",
            PlatformID.Win32NT => $"PLATFORM_WINDOWS",
            _ => throw new PlatformNotSupportedException()
        };

        ApplicationMacros["SE_APPLICATION"] = $"\"{SlnGenerator.Solution.CompiledRule.Name}\";";
        ApplicationMacros["SE_APPLICATION_TARGET"] = $"\"{SlnGenerator.PrimaryModule.CompiledRule.TargetName}\";";
        ApplicationMacros[PlatformId] = "1";

        // CMake always generate StaticLinkLibrary.
        ApplicationMacros["PLATFORM_STATIC_LIBRARY"] = "1";

        static string ReplaceEscape(string S) => S.Replace("\\", "\\\\");
        ApplicationMacros["ENGINE_ROOT"] = $"\"{ReplaceEscape(Path.GetFullPath(SlnGenerator.Solution.CompiledRule.EngineRoot))}\"";
        ApplicationMacros["GAME_ROOT"] = $"\"{ReplaceEscape(Path.GetFullPath(Environment.CurrentDirectory))}\"";

        // CMake always generate with Release mode.
        ApplicationMacros["DO_CHECK"] = "0";
        ApplicationMacros["SHIPPING"] = "1";

        StringBuilder Builder = new();

        Builder.AppendLine($"# Generated by build tools.");
        Builder.AppendLine($"");
        Builder.AppendLine($"cmake_minimum_required(VERSION 3.8)");
        Builder.AppendLine($"");
        Builder.AppendLine($"project({CompiledSolution.CompiledRule.Name})");
        Builder.AppendLine($"");
        Builder.AppendLine($"set(CMAKE_CXX_STANDARD_REQUIRED ON)");
        Builder.AppendLine($"set(CMAKE_CXX_STANDARD 20)");
        Builder.AppendLine($"set(CMAKE_CXX_FLAGS \"${{CMAKE_CXX_FLAGS}} -std=c++20 -fconcepts -fcoroutines -pthread\")");
        Builder.AppendLine($"");
        foreach (var (Name, Value) in ApplicationMacros)
        {
            Builder.AppendLine($"add_compile_definitions({Name}={Value})");
        }
        Builder.AppendLine($"");
        foreach (var Key in SortedProjects)
        {
            var Project = GeneratedProjects[Key];
            Builder.AppendLine($"add_subdirectory({Project.ProjectFile.GetParent()})");
        }

        SolutionFile = new FileReference("CMakeLists.txt");
        CMakeLists = Builder.ToString();
    }

    string CMakeLists;
    public FileReference SolutionFile { get; private set; }

    public void SaveAll()
    {
        foreach (var (_, CMakeProject) in GeneratedProjects)
        {
            CMakeProject.SaveAll();
        }

        File.WriteAllText(SolutionFile.FullPath, CMakeLists);
    }
}