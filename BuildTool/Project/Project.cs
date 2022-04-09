// Copyright 2020-2021 Aumoa.lib. All right reserved.

using CodeProjectConfiguration;

using DotNETUtilities;

using System.Text;

namespace BuildTool;

class Project : CompiledModuleCode<ModuleRule>
{
    public readonly DirectoryReference ProjectDir;

    public Project(FileReference ProjectRuleCode) : base(ProjectRuleCode)
    {
        ProjectDir = RuleCode.GetParent();
    }

    public string GenerateModuleInclude()
    {
        List<string> PublicCodes = new();

        foreach (var IncludePath in CompiledRule.PublicIncludePaths)
        {
            PublicCodes.AddRange(Directory.GetFiles(IncludePath, "*.h", SearchOption.AllDirectories).Select(p =>
            {
                string Full = Path.GetFullPath(p).Replace('\\', '/');
                return Full;
            }));
        }

        //FileReference ModulePCH = new(Path.Combine(ProjectPath, RuleCode.Name));
        StringBuilder ModulePCH = new();
        foreach (var HeaderFile in PublicCodes.Distinct())
        {
            ModulePCH.AppendLine($"#include \"{HeaderFile}\"");
        }

        return ModulePCH.ToString();
    }
}