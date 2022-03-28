// Copyright 2020-2022 Aumoa.lib. All right reserved.

using CodeProjectConfiguration;

using DotNETUtilities;

using System.Text;

namespace BuildTool;

class GeneratedCMakeProject : IGeneratedProject
{
    public GeneratedCMakeProject(Project CompiledProject)
    {
        this.CompiledProject = CompiledProject;
    }

    string GeneratedCMakeLists;
    bool bGenerated;
    string IntermediateProjectPath;

    List<GeneratedCMakeProject> PublicDependencyModules = new();
    List<GeneratedCMakeProject> PrivateDependencyModules = new();
    List<GeneratedCMakeProject> DependencyModules = new();

    public void Generate(CMakeGenerator SlnGenerator)
    {
        if (bGenerated)
        {
            return;
        }

        ModuleRule Rule = CompiledProject.CompiledRule;

        // Make dependencies.
        List<GeneratedCMakeProject> PublicDependencyModules = new();
        List<GeneratedCMakeProject> PrivateDependencyModules = new();

        void MakeDependencyModules(IList<GeneratedCMakeProject> List, IList<string> Names)
        {
            foreach (var ModuleName in Names)
            {
                if (!SlnGenerator.GeneratedProjects.TryGetValue(ModuleName, out var DependencyModule))
                {
                    Console.WriteLine("Couldn't find dependency module '{0}' while load '{1}' project.", ModuleName, Rule.Name);
                    continue;
                }

                DependencyModule.Generate(SlnGenerator);
                List.Add(DependencyModule);
            }
        }

        MakeDependencyModules(PublicDependencyModules, CompiledProject.CompiledRule.PublicDependencyModuleNames);
        MakeDependencyModules(PrivateDependencyModules, CompiledProject.CompiledRule.PrivateDependencyModuleNames);

        foreach (var Module in PublicDependencyModules)
        {
            this.PublicDependencyModules.AddRange(Module.PublicDependencyModules);
        }

        foreach (var Module in PrivateDependencyModules)
        {
            this.PrivateDependencyModules.AddRange(Module.PrivateDependencyModules);
        }

        this.PublicDependencyModules.AddRange(PublicDependencyModules);
        this.PublicDependencyModules = this.PublicDependencyModules.Distinct().ToList();

        this.PrivateDependencyModules.AddRange(PrivateDependencyModules);
        this.PrivateDependencyModules = this.PrivateDependencyModules.Distinct().ToList();

        DependencyModules.AddRange(this.PublicDependencyModules);
        DependencyModules.AddRange(this.PrivateDependencyModules);
        DependencyModules = DependencyModules.Distinct().ToList();

        GenerateRuntime(SlnGenerator);

        IntermediateProjectPath = "Intermediate/ProjectFiles";
        foreach (var Split in Rule.RelativePath.Split(".", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            IntermediateProjectPath = Path.Combine(IntermediateProjectPath, Split);
        }
        IntermediateProjectPath = Path.Combine(IntermediateProjectPath, CompiledProject.CompiledRule.Name);

        if (SlnGenerator.PrimaryModule.CompiledRule.Name == CompiledProject.CompiledRule.Name)
        {
            foreach (var Depend in DependencyModules)
            {
                SlnGenerator.SortedProjects.Add(Depend.CompiledProject.CompiledRule.Name);
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (CompiledProject.CompiledRule.ModuleType == ModuleType.ConsoleModule)
                {
                    SlnGenerator.SortedProjects.Add("LinuxCommon");
                    SlnGenerator.SortedProjects.Add("LinuxConsole");
                }
            }
            else
            {
                throw new Exception("Not supported platform.");
            }

            SlnGenerator.SortedProjects.Add(CompiledProject.CompiledRule.Name);
        }

        // Create directory.
        new DirectoryReference(IntermediateProjectPath).CreateIfNotExists(true);

        GenerateCMakeLists(SlnGenerator);

        bGenerated = true;
    }

    private List<string> AdditionalIncludeDirectories = new();
    private List<string> PreprocessorDefines = new();
    private List<string> DisableSpecificWarnings = new();
    private List<string> AdditionalLibraries = new();

    private readonly Dictionary<string, string> ApplicationMacros = new();

    private void GenerateRuntime(CMakeGenerator SlnGenerator)
    {
        foreach (var Module in DependencyModules)
        {
            var Rule = Module.CompiledProject.CompiledRule;

            AdditionalIncludeDirectories.AddRange(Rule.PublicIncludePaths);
            PreprocessorDefines.AddRange(Rule.PublicPreprocessorDefines);
            DisableSpecificWarnings.AddRange(Rule.PublicDisableWarnings.Select((x) => x.ToString()));
            AdditionalLibraries.AddRange(Rule.PublicAdditionalLibraries);

            if (Rule.ModuleType == ModuleType.GameModule || Rule.ModuleType == ModuleType.ConsoleModule)
            {
                PreprocessorDefines.Add(Rule.GetAPI(false, false));
            }
        }

        {
            var Rule = CompiledProject.CompiledRule;
            Rule.GenerateAdditionalIncludeDirectories(CompiledProject.ProjectDir.FullPath);
            Rule.GenerateAdditionalLibraries(CompiledProject.ProjectDir.FullPath);

            AdditionalIncludeDirectories.AddRange(Rule.PublicIncludePaths);
            AdditionalIncludeDirectories.AddRange(Rule.PrivateIncludePaths);
            AdditionalIncludeDirectories = AdditionalIncludeDirectories.Distinct().ToList();
            PreprocessorDefines.AddRange(Rule.PublicPreprocessorDefines);
            PreprocessorDefines.AddRange(Rule.PrivatePreprocessorDefines);
            PreprocessorDefines = PreprocessorDefines.Distinct().ToList();
            DisableSpecificWarnings.AddRange(Rule.PublicDisableWarnings.Select((x) => x.ToString()));
            DisableSpecificWarnings.AddRange(Rule.PrivateDisableWarnings.Select((x) => x.ToString()));
            DisableSpecificWarnings = DisableSpecificWarnings.Distinct().ToList();
            AdditionalLibraries.AddRange(Rule.PublicAdditionalLibraries);
            AdditionalLibraries.AddRange(Rule.PrivateAdditionalLibraries);
            AdditionalLibraries = AdditionalLibraries.Distinct().ToList();

            if (Rule.ModuleType == ModuleType.GameModule || Rule.ModuleType == ModuleType.ConsoleModule)
            {
                PreprocessorDefines.Add(Rule.GetAPI(true, false));
            }
        }

        // Project macros.
        ApplicationMacros["SE_ASSEMBLY"] = $"{CompiledProject.CompiledRule.Name}";
        ApplicationMacros["SE_ASSEMBLY_SAFE"] = $"{CompiledProject.CompiledRule.SafeName}";
        ApplicationMacros["SE_ASSEMBLY_NAME"] = $"\"{CompiledProject.CompiledRule.Name}\"";
        ApplicationMacros["SE_ASSEMBLY_INFO"] = $"{CompiledProject.CompiledRule.SafeName}_AssemblyInfo";
    }

    private void GenerateCMakeLists(CMakeGenerator SlnGenerator)
    {
        string ProjectName = CompiledProject.CompiledRule.Name;
        var SupportExts = new string[] { "*.cpp", "*.c", "*.ixx" };

        string SpecialLink = "";
        if (CompiledProject.CompiledRule.ModuleType == ModuleType.ConsoleApplication)
        {
            // Must be include whitespace.
            SpecialLink = " " + SlnGenerator.PrimaryModule.CompiledRule.Name;
        }

        StringBuilder Builder = new();

        Builder.AppendLine($"# Generated by build tools.");
        Builder.AppendLine($"");
        Builder.AppendLine($"cmake_minimum_required(VERSION 3.8)");
        Builder.AppendLine($"");
        Builder.AppendLine($"project({ProjectName})");
        Builder.AppendLine($"");
        foreach (var (Name, Value) in ApplicationMacros)
        {
            Builder.AppendLine($"add_compile_definitions({Name}={Value})");
        }
        Builder.AppendLine($"");
        foreach (var Define in PreprocessorDefines)
        {
            Builder.AppendLine($"add_compile_definitions({Define}=)");
        }
        Builder.AppendLine($"");
        Builder.AppendLine("set(CMAKE_LIBRARY_OUTPUT_DIRECTORY \"${CMAKE_SOURCE_DIR}/Build/Linux\")");
        Builder.AppendLine("set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY \"${CMAKE_SOURCE_DIR}/Build/Linux\")");
        Builder.AppendLine("set(CMAKE_RUNTIME_OUTPUT_DIRECTORY \"${CMAKE_SOURCE_DIR}/Build/Linux\")");
        Builder.AppendLine($"");
        Builder.AppendLine($"include_directories(");
        foreach (var Include in AdditionalIncludeDirectories)
        {
            Builder.AppendLine($"  {Include}");
        }
        Builder.AppendLine($")");
        Builder.AppendLine($"");
        Builder.AppendLine($"file(GLOB_RECURSE SRC_FILES");
        foreach (var Ext in SupportExts)
        {
            Builder.AppendLine($"  {CompiledProject.ProjectDir.FullPath}/Public/{Ext}");
            Builder.AppendLine($"  {CompiledProject.ProjectDir.FullPath}/Private/{Ext}");
        }
        Builder.AppendLine($")");
        Builder.AppendLine($"");
        switch (CompiledProject.CompiledRule.ModuleType)
        {
            case ModuleType.ConsoleModule:
            case ModuleType.GameModule:
                Builder.AppendLine($"add_library({ProjectName} STATIC ${{SRC_FILES}})");
                break;
            case ModuleType.Application:
            case ModuleType.ConsoleApplication:
                Builder.AppendLine($"add_executable({ProjectName} ${{SRC_FILES}})");
                break;
        }
        Builder.AppendLine($"");
        Builder.AppendLine($"target_link_libraries({ProjectName} {string.Join(' ', DependencyModules.Select(X => X.CompiledProject.CompiledRule.Name))}{SpecialLink})");

        string ProjectFilePath = Path.Combine(IntermediateProjectPath, "CMakeLists.txt");
        ProjectFile = new FileReference(ProjectFilePath);

        GeneratedCMakeLists = Builder.ToString();
    }

    public virtual void SaveAll()
    {
        File.WriteAllText(ProjectFile.FullPath, GeneratedCMakeLists);
    }

    public Project CompiledProject { get; private set; }

    virtual public FileReference ProjectFile { get; private set; }
}
