// Copyright 2020-2022 Aumoa.lib. All right reserved.

namespace BuildTool;

class CMakeGenerator : ISolutionGenerator
{
    public CMakeGenerator()
    {
    }

    public readonly Dictionary<string, GeneratedCMakeProject> GeneratedProjects = new();
    public readonly List<string> SortedProjects = new();

    public Solution Solution { get; private set; }
    public Project PrimaryModule { get; private set; }

    public virtual IGeneratedSolution Generate(Solution CompiledSolution)
    {
        Solution = CompiledSolution;

        foreach (var Project in CompiledSolution.AllProjects)
        {
            var GeneratedProject = new GeneratedCMakeProject(Project);
            GeneratedProjects.Add(GeneratedProject.CompiledProject.CompiledRule.Name, GeneratedProject);

            if (Project.CompiledRule.Name == CompiledSolution.CompiledRule.PrimaryModule)
            {
                PrimaryModule = Project;
            }
        }

        if (PrimaryModule == null)
        {
            throw new SolutionException("PrimaryModule is not specified in solution rule code.");
        }

        foreach (var (_, GeneratedProject) in GeneratedProjects)
        {
            GeneratedProject.Generate(this);
        }

        GeneratedCMakeSolution CMakeSolution = new(CompiledSolution);
        CMakeSolution.Generate(this);

        return CMakeSolution;
    }
}
