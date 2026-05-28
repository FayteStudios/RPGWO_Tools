using RPGWO.Core;
using RPGWO.Formats.Detection;
using RPGWO.Formats.Ini;
using RPGWO.Formats.Rpgwo;

namespace RPGWO.Tools.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return 1;
            }

            string command = args[0].Trim().ToLowerInvariant();

            return command switch
            {
                "scan" => RunScan(args),
                "parse" => RunParse(args),
                "roundtrip" => RunRoundtrip(args),

                "items" => RunItems(args),
                "itemdump" => RunItemDump(args),
                "itemset" => RunItemSet(args),
                "itemflag" => RunItemFlag(args),
                "itemremove" => RunItemRemove(args),

                "monsters" => RunMonsters(args),
                "monsterdump" => RunMonsterDump(args),
                "monsterset" => RunMonsterSet(args),
                "monsterflag" => RunMonsterFlag(args),
                "monsterremove" => RunMonsterRemove(args),

                "skills" => RunSkills(args),
                "skilldump" => RunSkillDump(args),
                "skillset" => RunSkillSet(args),
                "skillflag" => RunSkillFlag(args),
                "skillremove" => RunSkillRemove(args),

                "usages" => RunUsages(args),
                "usagedump" => RunUsageDump(args),
                "usageset" => RunUsageSet(args),
                "usageflag" => RunUsageFlag(args),
                "usageremove" => RunUsageRemove(args),

                "multiuses" => RunMultiUses(args),
                "multiusedump" => RunMultiUseDump(args),
                "multiuseset" => RunMultiUseSet(args),
                "multiuseflag" => RunMultiUseFlag(args),
                "multiuseremove" => RunMultiUseRemove(args),

                "magics" => RunMagics(args),
                "magicdump" => RunMagicDump(args),
                "magicset" => RunMagicSet(args),
                "magicflag" => RunMagicFlag(args),
                "magicremove" => RunMagicRemove(args),

                "treasures" => RunTreasures(args),
                "treasuredump" => RunTreasureDump(args),
                "treasureset" => RunTreasureSet(args),
                "treasureflag" => RunTreasureFlag(args),
                "treasureremove" => RunTreasureRemove(args),

                "world" => RunWorld(args),
                "worlddump" => RunWorldDump(args),
                "worldset" => RunWorldSet(args),
                "worldflag" => RunWorldFlag(args),
                "worldremove" => RunWorldRemove(args),

                "animations" => RunAnimations(args),
                "animationdump" => RunAnimationDump(args),
                "animationset" => RunAnimationSet(args),
                "animationflag" => RunAnimationFlag(args),
                "animationremove" => RunAnimationRemove(args),


                "help" or "-h" or "--help" => RunHelp(),
                _ => RunUnknownCommand(command)
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error:");
            Console.ResetColor();
            Console.WriteLine(ex.Message);
            return 1;
        }
    }

    private static int RunAnimationSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing animation.ini path, animation ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo animationset \"C:\\Path\\To\\animation.ini\" 1 Name \"New Animation Name\"");
            Console.WriteLine("  rpgwo animationset \"C:\\Path\\To\\animation.ini\" 1 Name \"New Animation Name\" \"C:\\Path\\To\\animation.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  animation.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int animationId))
        {
            Console.WriteLine($"Invalid animation ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = AnimationIniWriter.SetAnimationField(
            parseResult.Document,
            animationId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Animation update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Animation={animationId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        AnimationIniReadResult verifyResult = AnimationIniReader.ReadFile(outputPath);

        Console.WriteLine("Animation field updated.");
        Console.WriteLine($"Input:             {inputPath}");
        Console.WriteLine($"Output:            {outputPath}");
        Console.WriteLine($"Animation:         {animationId}");
        Console.WriteLine($"Field:             {fieldName}");
        Console.WriteLine($"Value:             {value}");
        Console.WriteLine($"Verify animations: {verifyResult.Animations.Count}");
        Console.WriteLine($"Verify issues:     {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:    {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunAnimationFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing animation.ini path, animation ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo animationflag \"C:\\Path\\To\\animation.ini\" 1 Rotational true");
            Console.WriteLine("  rpgwo animationflag \"C:\\Path\\To\\animation.ini\" 1 Rotational false");
            Console.WriteLine("  rpgwo animationflag \"C:\\Path\\To\\animation.ini\" 1 Rotational false \"C:\\Path\\To\\animation.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  animation.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int animationId))
        {
            Console.WriteLine($"Invalid animation ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = AnimationIniWriter.SetAnimationFlag(
            parseResult.Document,
            animationId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Animation flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Animation={animationId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        AnimationIniReadResult verifyResult = AnimationIniReader.ReadFile(outputPath);

        Console.WriteLine("Animation flag updated.");
        Console.WriteLine($"Input:             {inputPath}");
        Console.WriteLine($"Output:            {outputPath}");
        Console.WriteLine($"Animation:         {animationId}");
        Console.WriteLine($"Flag:              {flagName}");
        Console.WriteLine($"Enabled:           {enabled}");
        Console.WriteLine($"Verify animations: {verifyResult.Animations.Count}");
        Console.WriteLine($"Verify issues:     {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:    {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunAnimationRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing animation.ini path, animation ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo animationremove \"C:\\Path\\To\\animation.ini\" 1 Frame");
            Console.WriteLine("  rpgwo animationremove \"C:\\Path\\To\\animation.ini\" 1 Frame \"C:\\Path\\To\\animation.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  animation.updated.ini");
            Console.WriteLine();
            Console.WriteLine("Note: this removes all matching field lines inside the target animation block.");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int animationId))
        {
            Console.WriteLine($"Invalid animation ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = AnimationIniWriter.RemoveAnimationField(
            parseResult.Document,
            animationId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Animation field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Animation={animationId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        AnimationIniReadResult verifyResult = AnimationIniReader.ReadFile(outputPath);

        Console.WriteLine("Animation field removed.");
        Console.WriteLine($"Input:             {inputPath}");
        Console.WriteLine($"Output:            {outputPath}");
        Console.WriteLine($"Animation:         {animationId}");
        Console.WriteLine($"Field:             {fieldName}");
        Console.WriteLine($"Verify animations: {verifyResult.Animations.Count}");
        Console.WriteLine($"Verify issues:     {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:    {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunAnimations(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing animation.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo animations \"C:\\Path\\To\\ServerFolder\\animation.ini\"");
            return 1;
        }

        string path = args[1];

        AnimationIniReadResult result = AnimationIniReader.ReadFile(path);

        int animationsWithNames = result.Animations.Count(animation => !string.IsNullOrWhiteSpace(animation.Name));
        int unnamedAnimations = result.Animations.Count - animationsWithNames;
        int rotationalAnimations = result.Animations.Count(animation => animation.Rotational == true);
        int animationsWithSounds = result.Animations.Count(animation => animation.Sounds.Count > 0);
        int animationsWithFrameSizes = result.Animations.Count(animation => animation.FrameSizes.Count > 0);

        Console.WriteLine("Animation INI Read Result");
        Console.WriteLine("-------------------------");
        Console.WriteLine($"File:                   {path}");
        Console.WriteLine($"Animations loaded:      {result.Animations.Count}");
        Console.WriteLine($"Animations with names:  {animationsWithNames}");
        Console.WriteLine($"Unnamed animations:     {unnamedAnimations}");
        Console.WriteLine($"Rotational animations:  {rotationalAnimations}");
        Console.WriteLine($"Animations with sounds: {animationsWithSounds}");
        Console.WriteLine($"With frame sizes:       {animationsWithFrameSizes}");
        Console.WriteLine($"Global fields:          {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:           {result.GlobalFlags.Count}");
        Console.WriteLine($"Animation flags:        {result.FlagCount}");
        Console.WriteLine($"Frame lines:            {result.FrameCount}");
        Console.WriteLine($"FrameSize lines:        {result.FrameSizeCount}");
        Console.WriteLine($"Sound lines:            {result.SoundCount}");
        Console.WriteLine($"Unknown fields:         {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:           {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First animations");
        Console.WriteLine("----------------");

        foreach (var animation in result.Animations.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(animation.Name)
                ? "(unnamed)"
                : animation.Name;

            string rotational = animation.Rotational == true
                ? " Rotational"
                : "";

            string frames = animation.Frames.Count == 0
                ? ""
                : $" Frames: {string.Join("|", animation.Frames.Take(8))}";

            string sounds = animation.Sounds.Count == 0
                ? ""
                : $" Sounds: {string.Join("|", animation.Sounds.Take(3))}";

            Console.WriteLine($"{animation.Id,6}  {name}{rotational}{frames}{sounds}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Animations
            .SelectMany(animation => animation.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Animations
            .SelectMany(animation => animation.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top animation flags");
            Console.WriteLine("-------------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunAnimationDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing animation.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo animationdump \"C:\\Path\\To\\animation.ini\" \"C:\\Path\\To\\animations.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        AnimationIniReadResult result = AnimationIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "AnimationId",
            "Name",
            "Rotational",
            "Frames",
            "FrameSizes",
            "Sounds",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var animation in result.Animations.OrderBy(animation => animation.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(animation.Id),
                Csv(animation.Name),
                Csv(animation.Rotational),
                Csv(string.Join("|", animation.Frames)),
                Csv(string.Join("|", animation.FrameSizes)),
                Csv(string.Join("|", animation.Sounds)),
                Csv(string.Join("|", animation.Flags)),
                Csv(animation.UnknownFields.Count),
                Csv(animation.Issues.Count)));
        }

        Console.WriteLine("Animation CSV dump complete.");
        Console.WriteLine($"Input:              {inputPath}");
        Console.WriteLine($"Output:             {outputPath}");
        Console.WriteLine($"Animations dumped:  {result.Animations.Count}");
        Console.WriteLine($"Frame lines:        {result.FrameCount}");
        Console.WriteLine($"FrameSize lines:    {result.FrameSizeCount}");
        Console.WriteLine($"Sound lines:        {result.SoundCount}");
        Console.WriteLine($"Unknowns:           {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:             {result.Issues.Count}");

        return 0;
    }

    private static int RunWorldSet(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing world.ini path, setting key, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo worldset \"C:\\Path\\To\\world.ini\" ServerMessage \"New server message\"");
            Console.WriteLine("  rpgwo worldset \"C:\\Path\\To\\world.ini\" ServerMessage \"New server message\" \"C:\\Path\\To\\world.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  world.updated.ini");
            Console.WriteLine();
            Console.WriteLine("Note: if the key appears multiple times, this updates the last matching key.");
            return 1;
        }

        string inputPath = args[1];
        string key = args[2];
        string value = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = WorldIniWriter.SetWorldSetting(
            parseResult.Document,
            key,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("World setting update failed.");
            Console.ResetColor();
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        WorldIniReadResult verifyResult = WorldIniReader.ReadFile(outputPath);

        Console.WriteLine("World setting updated.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Key:             {key}");
        Console.WriteLine($"Value:           {value}");
        Console.WriteLine($"Verify settings: {verifyResult.World.Settings.Count}");
        Console.WriteLine($"Verify flags:    {verifyResult.World.Flags.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount + verifyResult.UnknownFlagCount}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunWorldFlag(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing world.ini path, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo worldflag \"C:\\Path\\To\\world.ini\" PopupMotd true");
            Console.WriteLine("  rpgwo worldflag \"C:\\Path\\To\\world.ini\" PopupMotd false");
            Console.WriteLine("  rpgwo worldflag \"C:\\Path\\To\\world.ini\" PopupMotd false \"C:\\Path\\To\\world.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  world.updated.ini");
            return 1;
        }

        string inputPath = args[1];
        string flagName = args[2];

        if (!TryParseCliBool(args[3], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[3]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = WorldIniWriter.SetWorldFlag(
            parseResult.Document,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("World flag update failed.");
            Console.ResetColor();
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        WorldIniReadResult verifyResult = WorldIniReader.ReadFile(outputPath);

        Console.WriteLine("World flag updated.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Flag:            {flagName}");
        Console.WriteLine($"Enabled:         {enabled}");
        Console.WriteLine($"Verify settings: {verifyResult.World.Settings.Count}");
        Console.WriteLine($"Verify flags:    {verifyResult.World.Flags.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount + verifyResult.UnknownFlagCount}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunWorldRemove(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing world.ini path or setting key.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo worldremove \"C:\\Path\\To\\world.ini\" ServerMessage");
            Console.WriteLine("  rpgwo worldremove \"C:\\Path\\To\\world.ini\" ServerMessage \"C:\\Path\\To\\world.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  world.updated.ini");
            Console.WriteLine();
            Console.WriteLine("Note: this removes all matching key=value settings.");
            return 1;
        }

        string inputPath = args[1];
        string key = args[2];

        string outputPath = args.Length >= 4
            ? args[3]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = WorldIniWriter.RemoveWorldSetting(
            parseResult.Document,
            key);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("World setting was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find setting '{key}'.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        WorldIniReadResult verifyResult = WorldIniReader.ReadFile(outputPath);

        Console.WriteLine("World setting removed.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Key:             {key}");
        Console.WriteLine($"Verify settings: {verifyResult.World.Settings.Count}");
        Console.WriteLine($"Verify flags:    {verifyResult.World.Flags.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount + verifyResult.UnknownFlagCount}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunWorld(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing world.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo world \"C:\\Path\\To\\ServerFolder\\world.ini\"");
            return 1;
        }

        string path = args[1];

        WorldIniReadResult result = WorldIniReader.ReadFile(path);

        int duplicateSettingKeys = result.World.Settings
            .GroupBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
            .Count(group => group.Count() > 1);

        Console.WriteLine("World INI Read Result");
        Console.WriteLine("---------------------");
        Console.WriteLine($"File:              {path}");
        Console.WriteLine($"Settings loaded:   {result.World.Settings.Count}");
        Console.WriteLine($"Known settings:    {result.KnownFieldCount}");
        Console.WriteLine($"Unknown settings:  {result.UnknownFieldCount}");
        Console.WriteLine($"Flags loaded:      {result.World.Flags.Count}");
        Console.WriteLine($"Known flags:       {result.KnownFlagCount}");
        Console.WriteLine($"Unknown flags:     {result.UnknownFlagCount}");
        Console.WriteLine($"Duplicate keys:    {duplicateSettingKeys}");
        Console.WriteLine($"Parse issues:      {result.Issues.Count}");
        Console.WriteLine();

        Console.WriteLine("First settings");
        Console.WriteLine("--------------");

        foreach (var setting in result.World.Settings.Take(25))
        {
            string known = setting.IsKnown ? "known" : "UNKNOWN";
            Console.WriteLine($"{setting.Key,-30} {setting.Value} [{known}]");
        }

        if (result.World.Settings.Count > 25)
            Console.WriteLine($"...and {result.World.Settings.Count - 25} more settings.");

        if (result.World.Flags.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Flags");
            Console.WriteLine("-----");

            foreach (string flag in result.World.Flags.Take(25))
            {
                string known = WorldIniReader.IsKnownFlag(flag) ? "known" : "UNKNOWN";
                Console.WriteLine($"{flag,-30} [{known}]");
            }

            if (result.World.Flags.Count > 25)
                Console.WriteLine($"...and {result.World.Flags.Count - 25} more flags.");
        }

        if (result.World.UnknownFields.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Unknown settings");
            Console.WriteLine("----------------");
            Console.ResetColor();

            var topUnknownFields = result.World.UnknownFields
                .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Take(50);

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        if (result.World.UnknownFlags.Count > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Unknown flags");
            Console.WriteLine("-------------");
            Console.ResetColor();

            foreach (string flag in result.World.UnknownFlags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(flag => flag, StringComparer.OrdinalIgnoreCase)
                .Take(50))
            {
                Console.WriteLine(flag);
            }

            int remaining = result.World.UnknownFlags
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() - 50;

            if (remaining > 0)
                Console.WriteLine($"...and {remaining} more unknown flags.");
        }

        if (duplicateSettingKeys > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Duplicate setting keys");
            Console.WriteLine("----------------------");

            var duplicates = result.World.Settings
                .GroupBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Take(25);

            foreach (var group in duplicates)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");

            int remainingDuplicates = result.World.Settings
                .GroupBy(setting => setting.Key, StringComparer.OrdinalIgnoreCase)
                .Count(group => group.Count() > 1) - 25;

            if (remainingDuplicates > 0)
                Console.WriteLine($"...and {remainingDuplicates} more duplicate keys.");
        }

        return 0;
    }

    private static int RunWorldDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing world.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo worlddump \"C:\\Path\\To\\world.ini\" \"C:\\Path\\To\\world.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        WorldIniReadResult result = WorldIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "LineNumber",
            "Type",
            "Key",
            "Value",
            "Known",
            "OriginalText"));

        foreach (var setting in result.World.Settings)
        {
            writer.WriteLine(string.Join(",",
                Csv(setting.LineNumber),
                Csv("Setting"),
                Csv(setting.Key),
                Csv(setting.Value),
                Csv(setting.IsKnown),
                Csv(setting.OriginalText)));
        }

        foreach (string flag in result.World.Flags)
        {
            writer.WriteLine(string.Join(",",
                Csv(""),
                Csv("Flag"),
                Csv(flag),
                Csv(""),
                Csv(WorldIniReader.IsKnownFlag(flag)),
                Csv(flag)));
        }

        Console.WriteLine("World CSV dump complete.");
        Console.WriteLine($"Input:            {inputPath}");
        Console.WriteLine($"Output:           {outputPath}");
        Console.WriteLine($"Settings dumped:  {result.World.Settings.Count}");
        Console.WriteLine($"Flags dumped:     {result.World.Flags.Count}");
        Console.WriteLine($"Unknown settings: {result.UnknownFieldCount}");
        Console.WriteLine($"Unknown flags:    {result.UnknownFlagCount}");
        Console.WriteLine($"Issues:           {result.Issues.Count}");

        return 0;
    }

    private static int RunTreasureSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing treasure.ini path, treasure table ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo treasureset \"C:\\Path\\To\\treasure.ini\" 1 Cost 250");
            Console.WriteLine("  rpgwo treasureset \"C:\\Path\\To\\treasure.ini\" 1 Cost 250 \"C:\\Path\\To\\treasure.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  treasure.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int treasureId))
        {
            Console.WriteLine($"Invalid treasure table ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = TreasureIniWriter.SetTreasureField(
            parseResult.Document,
            treasureId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Treasure update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Treasure table #{treasureId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        TreasureIniReadResult verifyResult = TreasureIniReader.ReadFile(outputPath);

        Console.WriteLine("Treasure field updated.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Treasure table:  {treasureId}");
        Console.WriteLine($"Field:           {fieldName}");
        Console.WriteLine($"Value:           {value}");
        Console.WriteLine($"Verify tables:   {verifyResult.Treasures.Count}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunTreasureFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing treasure.ini path, treasure table ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo treasureflag \"C:\\Path\\To\\treasure.ini\" 1 Random true");
            Console.WriteLine("  rpgwo treasureflag \"C:\\Path\\To\\treasure.ini\" 1 Random false");
            Console.WriteLine("  rpgwo treasureflag \"C:\\Path\\To\\treasure.ini\" 1 Random false \"C:\\Path\\To\\treasure.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  treasure.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int treasureId))
        {
            Console.WriteLine($"Invalid treasure table ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = TreasureIniWriter.SetTreasureFlag(
            parseResult.Document,
            treasureId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Treasure flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Treasure table #{treasureId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        TreasureIniReadResult verifyResult = TreasureIniReader.ReadFile(outputPath);

        Console.WriteLine("Treasure flag updated.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Treasure table:  {treasureId}");
        Console.WriteLine($"Flag:            {flagName}");
        Console.WriteLine($"Enabled:         {enabled}");
        Console.WriteLine($"Verify tables:   {verifyResult.Treasures.Count}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunTreasureRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing treasure.ini path, treasure table ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo treasureremove \"C:\\Path\\To\\treasure.ini\" 1 Cost");
            Console.WriteLine("  rpgwo treasureremove \"C:\\Path\\To\\treasure.ini\" 1 Cost \"C:\\Path\\To\\treasure.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  treasure.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int treasureId))
        {
            Console.WriteLine($"Invalid treasure table ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = TreasureIniWriter.RemoveTreasureField(
            parseResult.Document,
            treasureId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Treasure field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Treasure table #{treasureId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        TreasureIniReadResult verifyResult = TreasureIniReader.ReadFile(outputPath);

        Console.WriteLine("Treasure field removed.");
        Console.WriteLine($"Input:           {inputPath}");
        Console.WriteLine($"Output:          {outputPath}");
        Console.WriteLine($"Treasure table:  {treasureId}");
        Console.WriteLine($"Field:           {fieldName}");
        Console.WriteLine($"Verify tables:   {verifyResult.Treasures.Count}");
        Console.WriteLine($"Verify issues:   {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:  {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunTreasures(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing treasure.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo treasures \"C:\\Path\\To\\ServerFolder\\treasure.ini\"");
            return 1;
        }

        string path = args[1];

        TreasureIniReadResult result = TreasureIniReader.ReadFile(path);

        int totalFlags = result.Treasures.Sum(treasure => treasure.Flags.Count);
        int treasuresWithNames = result.Treasures.Count(treasure => !string.IsNullOrWhiteSpace(treasure.Name));
        int unnamedTreasures = result.Treasures.Count - treasuresWithNames;
        int treasuresWithItems = result.Treasures.Count(treasure => treasure.Items.Count > 0);
        int treasuresWithGroups = result.Treasures.Count(treasure => treasure.Groups.Count > 0 || treasure.Catagories.Count > 0);
        int treasuresWithNestedTables = result.Treasures.Count(treasure => treasure.TreasureRefs.Count > 0);

        Console.WriteLine("Treasure INI Read Result");
        Console.WriteLine("------------------------");
        Console.WriteLine($"File:                 {path}");
        Console.WriteLine($"Treasures loaded:     {result.Treasures.Count}");
        Console.WriteLine($"Treasures with names: {treasuresWithNames}");
        Console.WriteLine($"Unnamed treasures:    {unnamedTreasures}");
        Console.WriteLine($"With items:           {treasuresWithItems}");
        Console.WriteLine($"With groups/category: {treasuresWithGroups}");
        Console.WriteLine($"With nested tables:   {treasuresWithNestedTables}");
        Console.WriteLine($"Global fields:        {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:         {result.GlobalFlags.Count}");
        Console.WriteLine($"Treasure flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:       {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:         {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First treasures");
        Console.WriteLine("---------------");

        foreach (var treasure in result.Treasures.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(treasure.Name)
                ? "(unnamed)"
                : treasure.Name;

            string items = treasure.Items.Count == 0
                ? ""
                : $" Items: {string.Join("|", treasure.Items.Take(5))}";

            string groups = treasure.Groups.Count == 0 && treasure.Catagories.Count == 0
                ? ""
                : $" Groups: {string.Join("|", treasure.Groups.Take(3).Concat(treasure.Catagories.Take(3)))}";

            string flags = treasure.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", treasure.Flags.Take(5))}";

            Console.WriteLine($"{treasure.Id,6}  {name}{items}{groups}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Treasures
            .SelectMany(treasure => treasure.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Treasures
            .SelectMany(treasure => treasure.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top treasure flags");
            Console.WriteLine("------------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunTreasureDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing treasure.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo treasuredump \"C:\\Path\\To\\treasure.ini\" \"C:\\Path\\To\\treasures.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        TreasureIniReadResult result = TreasureIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "TreasureId",
            "Name",
            "TreasureCount",
            "Gold",
            "GoldMin",
            "GoldMax",
            "Money",
            "MoneyMin",
            "MoneyMax",
            "Chance",
            "Quantity",
            "QuantityMin",
            "QuantityMax",
            "Items",
            "ItemQuantities",
            "ItemQuantityMins",
            "ItemQuantityMaxes",
            "ItemChances",
            "ItemData1",
            "ItemData2",
            "ItemData3",
            "ItemData4",
            "ItemTexts",
            "ItemTotalUses",
            "Groups",
            "GroupChances",
            "GroupQuantities",
            "Catagories",
            "CatagoryChances",
            "CatagoryQuantities",
            "TreasureRefs",
            "TreasureRefChances",
            "TreasureRefQuantities",
            "Flags",
            "UnknownFieldCount",
            "TreasureName",
            "SkillIds",
            "SkillLows",
            "SkillHighs",
            "SpellIds",
            "SpellData",
            "Cost",
            "IssueCount"));

        foreach (var treasure in result.Treasures.OrderBy(treasure => treasure.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(treasure.Id),
                Csv(treasure.Name),
                Csv(treasure.TreasureName),
                Csv(string.Join("|", treasure.SkillIds)),
                Csv(string.Join("|", treasure.SkillLows)),
                Csv(string.Join("|", treasure.SkillHighs)),
                Csv(string.Join("|", treasure.SpellIds)),
                Csv(string.Join("|", treasure.SpellData)),
                Csv(treasure.Cost),
                Csv(treasure.TreasureCount),
                Csv(treasure.Gold),
                Csv(treasure.GoldMin),
                Csv(treasure.GoldMax),
                Csv(treasure.Money),
                Csv(treasure.MoneyMin),
                Csv(treasure.MoneyMax),
                Csv(treasure.Chance),
                Csv(treasure.Quantity),
                Csv(treasure.QuantityMin),
                Csv(treasure.QuantityMax),
                Csv(string.Join("|", treasure.Items)),
                Csv(string.Join("|", treasure.ItemQuantities)),
                Csv(string.Join("|", treasure.ItemQuantityMins)),
                Csv(string.Join("|", treasure.ItemQuantityMaxes)),
                Csv(string.Join("|", treasure.ItemChances)),
                Csv(string.Join("|", treasure.ItemData1)),
                Csv(string.Join("|", treasure.ItemData2)),
                Csv(string.Join("|", treasure.ItemData3)),
                Csv(string.Join("|", treasure.ItemData4)),
                Csv(string.Join("|", treasure.ItemTexts)),
                Csv(string.Join("|", treasure.ItemTotalUses)),
                Csv(string.Join("|", treasure.Groups)),
                Csv(string.Join("|", treasure.GroupChances)),
                Csv(string.Join("|", treasure.GroupQuantities)),
                Csv(string.Join("|", treasure.Catagories)),
                Csv(string.Join("|", treasure.CatagoryChances)),
                Csv(string.Join("|", treasure.CatagoryQuantities)),
                Csv(string.Join("|", treasure.TreasureRefs)),
                Csv(string.Join("|", treasure.TreasureRefChances)),
                Csv(string.Join("|", treasure.TreasureRefQuantities)),
                Csv(string.Join("|", treasure.Flags)),
                Csv(treasure.UnknownFields.Count),
                Csv(treasure.Issues.Count)));
        }

        Console.WriteLine("Treasure CSV dump complete.");
        Console.WriteLine($"Input:            {inputPath}");
        Console.WriteLine($"Output:           {outputPath}");
        Console.WriteLine($"Treasures dumped: {result.Treasures.Count}");
        Console.WriteLine($"Unknowns:         {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:           {result.Issues.Count}");

        return 0;
    }



    private static int RunMagics(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing magic.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo magics \"C:\\Path\\To\\ServerFolder\\magic.ini\"");
            return 1;
        }

        string path = args[1];

        MagicIniReadResult result = MagicIniReader.ReadFile(path);

        int totalFlags = result.Spells.Sum(spell => spell.Flags.Count);
        int spellsWithNames = result.Spells.Count(spell => !string.IsNullOrWhiteSpace(spell.Name));
        int unnamedSpells = result.Spells.Count - spellsWithNames;
        int spellsWithSkill = result.Spells.Count(spell => !string.IsNullOrWhiteSpace(spell.Skill));
        int spellsWithRunes = result.Spells.Count(spell => spell.Runes.Count > 0);

        Console.WriteLine("Magic INI Read Result");
        Console.WriteLine("---------------------");
        Console.WriteLine($"File:              {path}");
        Console.WriteLine($"Spells loaded:     {result.Spells.Count}");
        Console.WriteLine($"Spells with names: {spellsWithNames}");
        Console.WriteLine($"Unnamed spells:    {unnamedSpells}");
        Console.WriteLine($"With skills:       {spellsWithSkill}");
        Console.WriteLine($"With runes:        {spellsWithRunes}");
        Console.WriteLine($"Global fields:     {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:      {result.GlobalFlags.Count}");
        Console.WriteLine($"Spell flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:    {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:      {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First spells");
        Console.WriteLine("------------");

        foreach (var spell in result.Spells.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(spell.Name)
                ? "(unnamed)"
                : spell.Name;

            string skill = string.IsNullOrWhiteSpace(spell.Skill)
                ? ""
                : $" Skill: {spell.Skill}";

            string runes = spell.Runes.Count == 0
                ? ""
                : $" Runes: {string.Join("|", spell.Runes.Take(5))}";

            string flags = spell.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", spell.Flags.Take(5))}";

            Console.WriteLine($"{spell.Id,6}  {name}{skill}{runes}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Spells
            .SelectMany(spell => spell.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Spells
            .SelectMany(spell => spell.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top magic flags");
            Console.WriteLine("---------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunMagicSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing magic.ini path, spell ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo magicset \"C:\\Path\\To\\magic.ini\" 1 Name \"New Spell Name\"");
            Console.WriteLine("  rpgwo magicset \"C:\\Path\\To\\magic.ini\" 1 Name \"New Spell Name\" \"C:\\Path\\To\\magic.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  magic.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int spellId))
        {
            Console.WriteLine($"Invalid spell ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MagicIniWriter.SetMagicField(
            parseResult.Document,
            spellId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Magic update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Spell={spellId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MagicIniReadResult verifyResult = MagicIniReader.ReadFile(outputPath);

        Console.WriteLine("Magic field updated.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"Spell:         {spellId}");
        Console.WriteLine($"Field:         {fieldName}");
        Console.WriteLine($"Value:         {value}");
        Console.WriteLine($"Verify spells: {verifyResult.Spells.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMagicFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing magic.ini path, spell ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo magicflag \"C:\\Path\\To\\magic.ini\" 4 LineOfSight true");
            Console.WriteLine("  rpgwo magicflag \"C:\\Path\\To\\magic.ini\" 4 LineOfSight false");
            Console.WriteLine("  rpgwo magicflag \"C:\\Path\\To\\magic.ini\" 4 LineOfSight false \"C:\\Path\\To\\magic.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  magic.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int spellId))
        {
            Console.WriteLine($"Invalid spell ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MagicIniWriter.SetMagicFlag(
            parseResult.Document,
            spellId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Magic flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Spell={spellId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MagicIniReadResult verifyResult = MagicIniReader.ReadFile(outputPath);

        Console.WriteLine("Magic flag updated.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"Spell:         {spellId}");
        Console.WriteLine($"Flag:          {flagName}");
        Console.WriteLine($"Enabled:       {enabled}");
        Console.WriteLine($"Verify spells: {verifyResult.Spells.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMagicRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing magic.ini path, spell ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo magicremove \"C:\\Path\\To\\magic.ini\" 1 Rune1");
            Console.WriteLine("  rpgwo magicremove \"C:\\Path\\To\\magic.ini\" 1 Rune1 \"C:\\Path\\To\\magic.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  magic.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int spellId))
        {
            Console.WriteLine($"Invalid spell ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = MagicIniWriter.RemoveMagicField(
            parseResult.Document,
            spellId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Magic field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Spell={spellId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MagicIniReadResult verifyResult = MagicIniReader.ReadFile(outputPath);

        Console.WriteLine("Magic field removed.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"Spell:         {spellId}");
        Console.WriteLine($"Field:         {fieldName}");
        Console.WriteLine($"Verify spells: {verifyResult.Spells.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }


    private static int RunMagicDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing magic.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo magicdump \"C:\\Path\\To\\magic.ini\" \"C:\\Path\\To\\magics.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        MagicIniReadResult result = MagicIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "SpellId",
            "Name",
            "Description",
            "Skill",
            "SkillToLearn",
            "SkillMin",
            "SkillMax",
            "WandUse",
            "ManaCost",
            "Range",
            "Target",
            "CastTime",
            "SuccessXp",
            "FailedXp",
            "Runes",
            "RuneUses",
            "Animations",
            "ProjectileAnimation",
            "Sound",
            "Variance",
            "Life",
            "LifeRenewal",
            "LifeSteal",
            "Stamina",
            "StaminaRenewal",
            "StaminaSteal",
            "Mana",
            "ManaRenewal",
            "ManaSteal",
            "Cure",
            "Ice",
            "Blind",
            "Hero",
            "Strength",
            "Dexterity",
            "Quickness",
            "Intelligence",
            "Wisdom",
            "Armor",
            "Improve",
            "EssenceSteal",
            "DamageType",
            "SpawnItems",
            "SpawnItemQuantities",
            "TransformFrom",
            "TransformTo",
            "GolemItem",
            "GolemMonster",
            "GolemSkill",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var spell in result.Spells.OrderBy(spell => spell.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(spell.Id),
                Csv(spell.Name),
                Csv(spell.Description),
                Csv(spell.Skill),
                Csv(spell.SkillToLearn),
                Csv(spell.SkillMin),
                Csv(spell.SkillMax),
                Csv(spell.WandUse),
                Csv(spell.ManaCost),
                Csv(spell.Range),
                Csv(spell.Target),
                Csv(spell.CastTime),
                Csv(spell.SuccessXp),
                Csv(spell.FailedXp),
                Csv(string.Join("|", spell.Runes)),
                Csv(string.Join("|", spell.RuneUses)),
                Csv(string.Join("|", spell.Animations)),
                Csv(spell.ProjectileAnimation),
                Csv(spell.Sound),
                Csv(spell.Variance),
                Csv(spell.Life),
                Csv(spell.LifeRenewal),
                Csv(spell.LifeSteal),
                Csv(spell.Stamina),
                Csv(spell.StaminaRenewal),
                Csv(spell.StaminaSteal),
                Csv(spell.Mana),
                Csv(spell.ManaRenewal),
                Csv(spell.ManaSteal),
                Csv(spell.Cure),
                Csv(spell.Ice),
                Csv(spell.Blind),
                Csv(spell.Hero),
                Csv(spell.Strength),
                Csv(spell.Dexterity),
                Csv(spell.Quickness),
                Csv(spell.Intelligence),
                Csv(spell.Wisdom),
                Csv(spell.Armor),
                Csv(spell.Improve),
                Csv(spell.EssenceSteal),
                Csv(spell.DamageType),
                Csv(string.Join("|", spell.SpawnItems)),
                Csv(string.Join("|", spell.SpawnItemQuantities)),
                Csv(spell.TransformFrom),
                Csv(spell.TransformTo),
                Csv(spell.GolemItem),
                Csv(spell.GolemMonster),
                Csv(spell.GolemSkill),
                Csv(string.Join("|", spell.Flags)),
                Csv(spell.UnknownFields.Count),
                Csv(spell.Issues.Count)));
        }

        Console.WriteLine("Magic CSV dump complete.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"Spells dumped: {result.Spells.Count}");
        Console.WriteLine($"Unknowns:      {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:        {result.Issues.Count}");

        return 0;
    }


    private static int RunMultiUseSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing multiuse.ini path, recipe ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo multiuseset \"C:\\Path\\To\\multiuse.ini\" 1 Skill Blacksmith");
            Console.WriteLine("  rpgwo multiuseset \"C:\\Path\\To\\multiuse.ini\" 1 Skill Blacksmith \"C:\\Path\\To\\multiuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  multiuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int recipeId))
        {
            Console.WriteLine($"Invalid recipe ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MultiUseIniWriter.SetMultiUseField(
            parseResult.Document,
            recipeId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("MultiUse update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find MultiUse block RecipeId={recipeId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MultiUseIniReadResult verifyResult = MultiUseIniReader.ReadFile(outputPath);

        Console.WriteLine("MultiUse field updated.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"RecipeId:       {recipeId}");
        Console.WriteLine($"Field:          {fieldName}");
        Console.WriteLine($"Value:          {value}");
        Console.WriteLine($"Verify recipes: {verifyResult.Recipes.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown: {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMultiUseFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing multiuse.ini path, recipe ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo multiuseflag \"C:\\Path\\To\\multiuse.ini\" 1 SomeFlag true");
            Console.WriteLine("  rpgwo multiuseflag \"C:\\Path\\To\\multiuse.ini\" 1 SomeFlag false");
            Console.WriteLine("  rpgwo multiuseflag \"C:\\Path\\To\\multiuse.ini\" 1 SomeFlag false \"C:\\Path\\To\\multiuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  multiuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int recipeId))
        {
            Console.WriteLine($"Invalid recipe ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MultiUseIniWriter.SetMultiUseFlag(
            parseResult.Document,
            recipeId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("MultiUse flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find MultiUse block RecipeId={recipeId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MultiUseIniReadResult verifyResult = MultiUseIniReader.ReadFile(outputPath);

        Console.WriteLine("MultiUse flag updated.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"RecipeId:       {recipeId}");
        Console.WriteLine($"Flag:           {flagName}");
        Console.WriteLine($"Enabled:        {enabled}");
        Console.WriteLine($"Verify recipes: {verifyResult.Recipes.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown: {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMultiUseRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing multiuse.ini path, recipe ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo multiuseremove \"C:\\Path\\To\\multiuse.ini\" 1 Skill");
            Console.WriteLine("  rpgwo multiuseremove \"C:\\Path\\To\\multiuse.ini\" 1 Skill \"C:\\Path\\To\\multiuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  multiuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int recipeId))
        {
            Console.WriteLine($"Invalid recipe ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = MultiUseIniWriter.RemoveMultiUseField(
            parseResult.Document,
            recipeId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("MultiUse field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside MultiUse block RecipeId={recipeId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MultiUseIniReadResult verifyResult = MultiUseIniReader.ReadFile(outputPath);

        Console.WriteLine("MultiUse field removed.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"RecipeId:       {recipeId}");
        Console.WriteLine($"Field:          {fieldName}");
        Console.WriteLine($"Verify recipes: {verifyResult.Recipes.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown: {verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }


    private static int RunMultiUses(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing multiuse.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo multiuses \"C:\\Path\\To\\ServerFolder\\multiuse.ini\"");
            return 1;
        }

        string path = args[1];

        MultiUseIniReadResult result = MultiUseIniReader.ReadFile(path);

        int recipesWithSuccess = result.Recipes.Count(recipe => !string.IsNullOrWhiteSpace(recipe.SuccessItem));
        int recipesWithFocus = result.Recipes.Count(recipe => !string.IsNullOrWhiteSpace(recipe.FocusItem));
        int recipesWithSkills = result.Recipes.Count(recipe => !string.IsNullOrWhiteSpace(recipe.Skill));

        Console.WriteLine("MultiUse INI Read Result");
        Console.WriteLine("------------------------");
        Console.WriteLine($"File:              {path}");
        Console.WriteLine($"Recipes loaded:    {result.Recipes.Count}");
        Console.WriteLine($"With success item: {recipesWithSuccess}");
        Console.WriteLine($"With focus item:   {recipesWithFocus}");
        Console.WriteLine($"With skills:       {recipesWithSkills}");
        Console.WriteLine($"Global fields:     {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:      {result.GlobalFlags.Count}");
        Console.WriteLine($"Recipe flags:      {result.FlagCount}");
        Console.WriteLine($"Unknown fields:    {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:      {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First recipes");
        Console.WriteLine("-------------");

        foreach (var recipe in result.Recipes.Take(25))
        {
            string success = string.IsNullOrWhiteSpace(recipe.SuccessItem)
                ? "(no success item)"
                : recipe.SuccessItem;

            string focus = string.IsNullOrWhiteSpace(recipe.FocusItem)
                ? ""
                : $" Focus: {recipe.FocusItem}";

            string skill = string.IsNullOrWhiteSpace(recipe.Skill)
                ? ""
                : $" Skill: {recipe.Skill}";

            Console.WriteLine($"{recipe.Id,6}  {success}{focus}{skill}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Recipes
            .SelectMany(recipe => recipe.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunMultiUseDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing multiuse.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo multiusedump \"C:\\Path\\To\\multiuse.ini\" \"C:\\Path\\To\\multiuses.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        MultiUseIniReadResult result = MultiUseIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "RecipeId",
            "SuccessItem",
            "SuccessItemQuantity",
            "FocusItem",
            "NeedItems",
            "NeedItemQuantities",
            "ResultItems",
            "ResultItemQuantities",
            "Skill",
            "SkillMin",
            "SkillMax",
            "SkillXpSuccess",
            "StaminaCost",
            "SuccessMessage",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var recipe in result.Recipes.OrderBy(recipe => recipe.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(recipe.Id),
                Csv(recipe.SuccessItem),
                Csv(recipe.SuccessItemQuantity),
                Csv(recipe.FocusItem),
                Csv(string.Join("|", recipe.NeedItems)),
                Csv(string.Join("|", recipe.NeedItemQuantities)),
                Csv(string.Join("|", recipe.ResultItems)),
                Csv(string.Join("|", recipe.ResultItemQuantities)),
                Csv(recipe.Skill),
                Csv(recipe.SkillMin),
                Csv(recipe.SkillMax),
                Csv(recipe.SkillXpSuccess),
                Csv(recipe.StaminaCost),
                Csv(recipe.SuccessMessage),
                Csv(string.Join("|", recipe.Flags)),
                Csv(recipe.UnknownFields.Count),
                Csv(recipe.Issues.Count)));
        }

        Console.WriteLine("MultiUse CSV dump complete.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"Recipes dumped: {result.Recipes.Count}");
        Console.WriteLine($"Unknowns:       {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:         {result.Issues.Count}");

        return 0;
    }

    private static int RunUsageSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing itemuse.ini path, usage EntryId, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo usageset \"C:\\Path\\To\\itemuse.ini\" 1 Skill Farming");
            Console.WriteLine("  rpgwo usageset \"C:\\Path\\To\\itemuse.ini\" 1 Skill Farming \"C:\\Path\\To\\itemuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  itemuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int entryId))
        {
            Console.WriteLine($"Invalid usage EntryId: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = UsageIniWriter.SetUsageField(
            parseResult.Document,
            entryId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Usage update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Itemuse block EntryId={entryId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        UsageIniReadResult verifyResult = UsageIniReader.ReadFile(outputPath);

        Console.WriteLine("Usage field updated.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"EntryId:       {entryId}");
        Console.WriteLine($"Field:         {fieldName}");
        Console.WriteLine($"Value:         {value}");
        Console.WriteLine($"Verify usages: {verifyResult.Usages.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunUsageFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing itemuse.ini path, usage EntryId, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo usageflag \"C:\\Path\\To\\itemuse.ini\" 1 Ownland true");
            Console.WriteLine("  rpgwo usageflag \"C:\\Path\\To\\itemuse.ini\" 1 Ownland false");
            Console.WriteLine("  rpgwo usageflag \"C:\\Path\\To\\itemuse.ini\" 1 Ownland false \"C:\\Path\\To\\itemuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  itemuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int entryId))
        {
            Console.WriteLine($"Invalid usage EntryId: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = UsageIniWriter.SetUsageFlag(
            parseResult.Document,
            entryId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Usage flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Itemuse block EntryId={entryId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        UsageIniReadResult verifyResult = UsageIniReader.ReadFile(outputPath);

        Console.WriteLine("Usage flag updated.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"EntryId:       {entryId}");
        Console.WriteLine($"Flag:          {flagName}");
        Console.WriteLine($"Enabled:       {enabled}");
        Console.WriteLine($"Verify usages: {verifyResult.Usages.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunUsageRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing itemuse.ini path, usage EntryId, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo usageremove \"C:\\Path\\To\\itemuse.ini\" 1 SuccessItem1");
            Console.WriteLine("  rpgwo usageremove \"C:\\Path\\To\\itemuse.ini\" 1 SuccessItem1 \"C:\\Path\\To\\itemuse.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  itemuse.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int entryId))
        {
            Console.WriteLine($"Invalid usage EntryId: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = UsageIniWriter.RemoveUsageField(
            parseResult.Document,
            entryId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Usage field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Itemuse block EntryId={entryId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        UsageIniReadResult verifyResult = UsageIniReader.ReadFile(outputPath);

        Console.WriteLine("Usage field removed.");
        Console.WriteLine($"Input:         {inputPath}");
        Console.WriteLine($"Output:        {outputPath}");
        Console.WriteLine($"EntryId:       {entryId}");
        Console.WriteLine($"Field:         {fieldName}");
        Console.WriteLine($"Verify usages: {verifyResult.Usages.Count}");
        Console.WriteLine($"Verify issues: {verifyResult.Issues.Count}");
        Console.WriteLine($"Verify unknown:{verifyResult.UnknownFieldCount}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunUsages(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing itemuse.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo usages \"C:\\Path\\To\\ServerFolder\\itemuse.ini\"");
            return 1;
        }

        string path = args[1];

        UsageIniReadResult result = UsageIniReader.ReadFile(path);

        int totalFlags = result.Usages.Sum(usage => usage.Flags.Count);
        int usagesWithTools = result.Usages.Count(usage => !string.IsNullOrWhiteSpace(usage.ItemTool));
        int usagesWithFocus = result.Usages.Count(usage => !string.IsNullOrWhiteSpace(usage.ItemFocus));
        int usagesWithSkills = result.Usages.Count(usage => !string.IsNullOrWhiteSpace(usage.Skill));

        Console.WriteLine("Usage INI Read Result");
        Console.WriteLine("---------------------");
        Console.WriteLine($"File:              {path}");
        Console.WriteLine($"Usages loaded:     {result.Usages.Count}");
        Console.WriteLine($"With item tools:   {usagesWithTools}");
        Console.WriteLine($"With item focus:   {usagesWithFocus}");
        Console.WriteLine($"With skills:       {usagesWithSkills}");
        Console.WriteLine($"Global fields:     {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:      {result.GlobalFlags.Count}");
        Console.WriteLine($"Usage flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:    {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:      {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First usages");
        Console.WriteLine("------------");

        foreach (var usage in result.Usages.Take(25))
        {
            string tool = string.IsNullOrWhiteSpace(usage.ItemTool)
                ? "(no tool)"
                : usage.ItemTool;

            string focus = string.IsNullOrWhiteSpace(usage.ItemFocus)
                ? ""
                : $" + {usage.ItemFocus}";

            string skill = string.IsNullOrWhiteSpace(usage.Skill)
                ? ""
                : $" Skill: {usage.Skill}";

            string flags = usage.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", usage.Flags.Take(5))}";

            Console.WriteLine($"{usage.Id,6}  {tool}{focus}{skill}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Usages
            .SelectMany(usage => usage.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Usages
            .SelectMany(usage => usage.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top usage flags");
            Console.WriteLine("---------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunUsageDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing itemuse.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo usagedump \"C:\\Path\\To\\itemuse.ini\" \"C:\\Path\\To\\usages.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        UsageIniReadResult result = UsageIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "EntryId",
            "ItemTool",
            "ItemFocus",
            "FocusSubType",
            "ItemToolQuantity",
            "Skill",
            "SkillMin",
            "SkillMax",
            "SkillXpSuccess",
            "SkillXpFailure",
            "StaminaCost",
            "Range",
            "Animation",
            "SuccessTool",
            "SuccessFocus",
            "SuccessItems",
            "SuccessItemQuantities",
            "FailedTool",
            "FailedFocus",
            "FailedDamage",
            "FailedItems",
            "FailedItemQuantities",
            "NeedFlatSurface",
            "NeedUnLevelSurface",
            "SurfaceGround",
            "SurfaceUnderGround",
            "SurfaceWater",
            "UsePlayerPosition",
            "SuccessMessage",
            "FailedMessage",
            "MonsterId",
            "PlayerUsageTimeout",
            "GiveSkillBonus",
            "Guild",
            "Drunk",
            "Heal",
            "HealPoison",
            "Warp",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var usage in result.Usages.OrderBy(usage => usage.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(usage.Id),
                Csv(usage.ItemTool),
                Csv(usage.ItemFocus),
                Csv(usage.FocusSubType),
                Csv(usage.ItemToolQuantity),
                Csv(usage.Skill),
                Csv(usage.SkillMin),
                Csv(usage.SkillMax),
                Csv(usage.SkillXpSuccess),
                Csv(usage.SkillXpFailure),
                Csv(usage.StaminaCost),
                Csv(usage.Range),
                Csv(usage.Animation),
                Csv(usage.SuccessTool),
                Csv(usage.SuccessFocus),
                Csv(string.Join("|", usage.SuccessItems)),
                Csv(string.Join("|", usage.SuccessItemQuantities)),
                Csv(usage.FailedTool),
                Csv(usage.FailedFocus),
                Csv(usage.FailedDamage),
                Csv(string.Join("|", usage.FailedItems)),
                Csv(string.Join("|", usage.FailedItemQuantities)),
                Csv(usage.NeedFlatSurface),
                Csv(usage.NeedUnLevelSurface),
                Csv(usage.SurfaceGround),
                Csv(usage.SurfaceUnderGround),
                Csv(usage.SurfaceWater),
                Csv(usage.UsePlayerPosition),
                Csv(usage.SuccessMessage),
                Csv(usage.FailedMessage),
                Csv(usage.MonsterId),
                Csv(usage.PlayerUsageTimeout),
                Csv(usage.GiveSkillBonus),
                Csv(usage.Guild),
                Csv(usage.Drunk),
                Csv(usage.Heal),
                Csv(usage.HealPoison),
                Csv(usage.Warp),
                Csv(string.Join("|", usage.Flags)),
                Csv(usage.UnknownFields.Count),
                Csv(usage.Issues.Count)));
        }

        Console.WriteLine("Usage CSV dump complete.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Usages dumped:{result.Usages.Count}");
        Console.WriteLine($"Unknowns:     {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:       {result.Issues.Count}");

        return 0;
    }


    private static int RunSkillSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing skill.ini path, skill ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo skillset \"C:\\Path\\To\\skill.ini\" 1 Name \"New Skill Name\"");
            Console.WriteLine("  rpgwo skillset \"C:\\Path\\To\\skill.ini\" 1 Name \"New Skill Name\" \"C:\\Path\\To\\skill.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  skill.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int skillId))
        {
            Console.WriteLine($"Invalid skill ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SkillIniWriter.SetSkillField(
            parseResult.Document,
            skillId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Skill update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Skill={skillId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        SkillIniReadResult verifyResult = SkillIniReader.ReadFile(outputPath);

        Console.WriteLine("Skill field updated.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Skill:        {skillId}");
        Console.WriteLine($"Field:        {fieldName}");
        Console.WriteLine($"Value:        {value}");
        Console.WriteLine($"Verify skills:{verifyResult.Skills.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunSkillFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing skill.ini path, skill ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo skillflag \"C:\\Path\\To\\skill.ini\" 1 FreeSkill true");
            Console.WriteLine("  rpgwo skillflag \"C:\\Path\\To\\skill.ini\" 1 FreeSkill false");
            Console.WriteLine("  rpgwo skillflag \"C:\\Path\\To\\skill.ini\" 1 FreeSkill false \"C:\\Path\\To\\skill.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  skill.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int skillId))
        {
            Console.WriteLine($"Invalid skill ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = SkillIniWriter.SetSkillFlag(
            parseResult.Document,
            skillId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Skill flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Skill={skillId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        SkillIniReadResult verifyResult = SkillIniReader.ReadFile(outputPath);

        Console.WriteLine("Skill flag updated.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Skill:        {skillId}");
        Console.WriteLine($"Flag:         {flagName}");
        Console.WriteLine($"Enabled:      {enabled}");
        Console.WriteLine($"Verify skills:{verifyResult.Skills.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunSkillRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing skill.ini path, skill ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo skillremove \"C:\\Path\\To\\skill.ini\" 1 SkillPoints");
            Console.WriteLine("  rpgwo skillremove \"C:\\Path\\To\\skill.ini\" 1 SkillPoints \"C:\\Path\\To\\skill.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  skill.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int skillId))
        {
            Console.WriteLine($"Invalid skill ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = SkillIniWriter.RemoveSkillField(
            parseResult.Document,
            skillId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Skill field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Skill={skillId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        SkillIniReadResult verifyResult = SkillIniReader.ReadFile(outputPath);

        Console.WriteLine("Skill field removed.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Skill:        {skillId}");
        Console.WriteLine($"Field:        {fieldName}");
        Console.WriteLine($"Verify skills:{verifyResult.Skills.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunSkills(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing skill.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo skills \"C:\\Path\\To\\ServerFolder\\skill.ini\"");
            return 1;
        }

        string path = args[1];

        SkillIniReadResult result = SkillIniReader.ReadFile(path);

        int totalFlags = result.Skills.Sum(skill => skill.Flags.Count);
        int skillsWithNames = result.Skills.Count(skill => !string.IsNullOrWhiteSpace(skill.Name));
        int unnamedSkills = result.Skills.Count - skillsWithNames;

        Console.WriteLine("Skill INI Read Result");
        Console.WriteLine("---------------------");
        Console.WriteLine($"File:              {path}");
        Console.WriteLine($"Skills loaded:     {result.Skills.Count}");
        Console.WriteLine($"Skills with names: {skillsWithNames}");
        Console.WriteLine($"Unnamed skills:    {unnamedSkills}");
        Console.WriteLine($"Global fields:     {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:      {result.GlobalFlags.Count}");
        Console.WriteLine($"Skill flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:    {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:      {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        if (result.GlobalFlags.Count > 0)
        {
            Console.WriteLine("Global flags");
            Console.WriteLine("------------");

            foreach (string flag in result.GlobalFlags.Take(25))
                Console.WriteLine(flag);

            if (result.GlobalFlags.Count > 25)
                Console.WriteLine($"...and {result.GlobalFlags.Count - 25} more global flags.");

            Console.WriteLine();
        }

        Console.WriteLine("First skills");
        Console.WriteLine("------------");

        foreach (var skill in result.Skills.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(skill.Name)
                ? "(unnamed)"
                : skill.Name;

            string flags = skill.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", skill.Flags.Take(5))}";

            Console.WriteLine($"{skill.Id,6}  {name}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Skills
            .SelectMany(skill => skill.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Skills
            .SelectMany(skill => skill.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top skill flags");
            Console.WriteLine("---------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunSkillDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing skill.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo skilldump \"C:\\Path\\To\\skill.ini\" \"C:\\Path\\To\\skills.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        SkillIniReadResult result = SkillIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Usable",
            "SkillPoints",
            "Strength",
            "Dexterity",
            "Quickness",
            "Intelligence",
            "Wisdom",
            "Divisor",
            "BurdenFactor",
            "Description",
            "Purpose",
            "SpecialFeature",
            "FreeSkill",
            "LevelReq",
            "ExcludeSkill",
            "Flags",
            "UnknownFieldCount",
            "IssueCount"));

        foreach (var skill in result.Skills.OrderBy(skill => skill.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(skill.Id),
                Csv(skill.Name),
                Csv(skill.Usable),
                Csv(skill.SkillPoints),
                Csv(skill.Strength),
                Csv(skill.Dexterity),
                Csv(skill.Quickness),
                Csv(skill.Intelligence),
                Csv(skill.Wisdom),
                Csv(skill.Divisor),
                Csv(skill.BurdenFactor),
                Csv(skill.Description),
                Csv(skill.Purpose),
                Csv(skill.SpecialFeature),
                Csv(skill.FreeSkill),
                Csv(skill.LevelReq),
                Csv(skill.ExcludeSkill),
                Csv(string.Join("|", skill.Flags)),
                Csv(skill.UnknownFields.Count),
                Csv(skill.Issues.Count)));
        }

        Console.WriteLine("Skill CSV dump complete.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Skills dumped:{result.Skills.Count}");
        Console.WriteLine($"Unknowns:     {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:       {result.Issues.Count}");

        return 0;
    }

    private static int RunMonsterSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing monster.ini path, monster ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo monsterset \"C:\\Path\\To\\monster.ini\" 10 Name \"Giant Mega Roach\"");
            Console.WriteLine("  rpgwo monsterset \"C:\\Path\\To\\monster.ini\" 10 Name \"Giant Mega Roach\" \"C:\\Path\\To\\monster.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  monster.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int monsterId))
        {
            Console.WriteLine($"Invalid monster ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MonsterIniWriter.SetMonsterField(
            parseResult.Document,
            monsterId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Monster update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Monster={monsterId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MonsterIniReadResult verifyResult = MonsterIniReader.ReadFile(outputPath);

        Console.WriteLine("Monster field updated.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"Monster:        {monsterId}");
        Console.WriteLine($"Field:          {fieldName}");
        Console.WriteLine($"Value:          {value}");
        Console.WriteLine($"Verify monsters:{verifyResult.Monsters.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMonsterFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing monster.ini path, monster ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo monsterflag \"C:\\Path\\To\\monster.ini\" 23 HelpFriends true");
            Console.WriteLine("  rpgwo monsterflag \"C:\\Path\\To\\monster.ini\" 23 HelpFriends false");
            Console.WriteLine("  rpgwo monsterflag \"C:\\Path\\To\\monster.ini\" 23 HelpFriends false \"C:\\Path\\To\\monster.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  monster.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int monsterId))
        {
            Console.WriteLine($"Invalid monster ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = MonsterIniWriter.SetMonsterFlag(
            parseResult.Document,
            monsterId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Monster flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Monster={monsterId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MonsterIniReadResult verifyResult = MonsterIniReader.ReadFile(outputPath);

        Console.WriteLine("Monster flag updated.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"Monster:        {monsterId}");
        Console.WriteLine($"Flag:           {flagName}");
        Console.WriteLine($"Enabled:        {enabled}");
        Console.WriteLine($"Verify monsters:{verifyResult.Monsters.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMonsterRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing monster.ini path, monster ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo monsterremove \"C:\\Path\\To\\monster.ini\" 10 Treasure0");
            Console.WriteLine("  rpgwo monsterremove \"C:\\Path\\To\\monster.ini\" 10 Treasure0 \"C:\\Path\\To\\monster.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  monster.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int monsterId))
        {
            Console.WriteLine($"Invalid monster ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = MonsterIniWriter.RemoveMonsterField(
            parseResult.Document,
            monsterId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Monster field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Monster={monsterId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        MonsterIniReadResult verifyResult = MonsterIniReader.ReadFile(outputPath);

        Console.WriteLine("Monster field removed.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"Monster:        {monsterId}");
        Console.WriteLine($"Field:          {fieldName}");
        Console.WriteLine($"Verify monsters:{verifyResult.Monsters.Count}");
        Console.WriteLine($"Verify issues:  {verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunItemFlag(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing item.ini path, item ID, flag name, or enabled value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo itemflag \"C:\\Path\\To\\item.ini\" 100 Stackable true");
            Console.WriteLine("  rpgwo itemflag \"C:\\Path\\To\\item.ini\" 100 Stackable false");
            Console.WriteLine("  rpgwo itemflag \"C:\\Path\\To\\item.ini\" 100 Stackable false \"C:\\Path\\To\\item.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  item.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int itemId))
        {
            Console.WriteLine($"Invalid item ID: {args[2]}");
            return 1;
        }

        string flagName = args[3];

        if (!TryParseCliBool(args[4], out bool enabled))
        {
            Console.WriteLine($"Invalid enabled value: {args[4]}");
            Console.WriteLine("Use true/false, yes/no, on/off, or 1/0.");
            return 1;
        }

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = ItemIniWriter.SetItemFlag(
            parseResult.Document,
            itemId,
            flagName,
            enabled);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Item flag update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Item={itemId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        ItemIniReadResult verifyResult = ItemIniReader.ReadFile(outputPath);

        Console.WriteLine("Item flag updated.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Item:         {itemId}");
        Console.WriteLine($"Flag:         {flagName}");
        Console.WriteLine($"Enabled:      {enabled}");
        Console.WriteLine($"Verify items: {verifyResult.Items.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }

    private static int RunMonsterDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing monster.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo monsterdump \"C:\\Path\\To\\monster.ini\" \"C:\\Path\\To\\monsters.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        MonsterIniReadResult result = MonsterIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Class",
            "Type",
            "SubType",
            "Image",
            "Animation",
            "Animation0",
            "Animation1",
            "Animation2",
            "Animation3",
            "Flags",
            "UnknownFieldCount",
            "IssueCount",

            "Life",
            "Stamina",
            "Mana",
            "Strength",
            "Dexterity",
            "Quickness",
            "Intelligence",
            "Wisdom",

            "MagicDefense",
            "MeleeDefense",
            "MissleDefense",
            "ArmorLevel",
            "MagicArmorLevel",
            "MagicArmorLevelAlt",
            "FireArmorLevel",
            "ColdArmorLevel",
            "ElectricArmorLevel",
            "BashArmorLevel",
            "CutArmorLevel",
            "ThrustArmorLevel",

            "Weapon",
            "RangeWeapon",
            "Sheild",
            "ChestArmor",
            "HeadArmor",
            "LegArmor",
            "Unarmed",

            "Run",
            "Sword",
            "Dagger",
            "Bow",
            "Crossbow",
            "Throwing",
            "Axe",
            "Mace",
            "Flail",
            "Scythe",
            "Spear",
            "Sneak",
            "Stealth",

            "CastSpell",
            "CastHeal",
            "CastHarm",
            "CastNova",
            "CastHero",
            "CastIce",
            "CastBlackHole",
            "CastLightning",
            "MagicPower",

            "Catagory",
            "FriendCatagories",
            "EnemyCatagories",
            "Friends",

            "Treasures",
            "TreasureQuantities",
            "TreasureChances",
            "TreasureData1",
            "TreasureData2",
            "TreasureData3",
            "TreasureData4",
            "TreasureTotalUses",
            "TreasureTexts",

            "TradeGroups",
            "TradeGroupSellMaximums",
            "TradeBuyValue",
            "TradeSellValue",
            "TradeTalkFarewell",
            "TradeTalkSuccess",

            "TalkGreeting",
            "TalkIdle",
            "GreetingAnimation",

            "DeadItem",
            "ImageType",
            "Scan",
            "Roam",
            "RoamChance",
            "KeepDistance",
            "ChaseRange",
            "ChaseItem",
            "MoveSpeed",
            "SightRange",

            "SpawnItem",
            "SpawnItemChance",
            "SpawnItemTimeout",
            "GrowthMonster",
            "GrowthMonsterChance",
            "GrowthMonsterTimeout",
            "IdleTransformItem",
            "ItemTrail",

            "QuestTakeItems",
            "QuestTakeQuantities",
            "QuestTalkEntries",
            "QuestGiveItems",
            "QuestGiveQuantities",
            "QuestGiveExperience",
            "QuestGiveTames",
            "QuestGiveData1",
            "QuestGiveData2",
            "QuestGiveData3",
            "QuestGiveData4"));

        foreach (var monster in result.Monsters.OrderBy(monster => monster.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(monster.Id),
                Csv(monster.Name),
                Csv(monster.Class),
                Csv(monster.Type),
                Csv(monster.SubType),
                Csv(monster.Image),
                Csv(monster.Animation),
                Csv(monster.Animation0),
                Csv(monster.Animation1),
                Csv(monster.Animation2),
                Csv(monster.Animation3),
                Csv(string.Join("|", monster.Flags)),
                Csv(monster.UnknownFields.Count),
                Csv(monster.Issues.Count),

                Csv(monster.Life),
                Csv(monster.Stamina),
                Csv(monster.Mana),
                Csv(monster.Strength),
                Csv(monster.Dexterity),
                Csv(monster.Quickness),
                Csv(monster.Intelligence),
                Csv(monster.Wisdom),

                Csv(monster.MagicDefense),
                Csv(monster.MeleeDefense),
                Csv(monster.MissleDefense),
                Csv(monster.ArmorLevel),
                Csv(monster.MagicArmorLevel),
                Csv(monster.MagicArmorLevelAlt),
                Csv(monster.FireArmorLevel),
                Csv(monster.ColdArmorLevel),
                Csv(monster.ElectricArmorLevel),
                Csv(monster.BashArmorLevel),
                Csv(monster.CutArmorLevel),
                Csv(monster.ThrustArmorLevel),

                Csv(monster.Weapon),
                Csv(monster.RangeWeapon),
                Csv(monster.Sheild),
                Csv(monster.ChestArmor),
                Csv(monster.HeadArmor),
                Csv(monster.LegArmor),
                Csv(monster.Unarmed),

                Csv(monster.Run),
                Csv(monster.Sword),
                Csv(monster.Dagger),
                Csv(monster.Bow),
                Csv(monster.Crossbow),
                Csv(monster.Throwing),
                Csv(monster.Axe),
                Csv(monster.Mace),
                Csv(monster.Flail),
                Csv(monster.Scythe),
                Csv(monster.Spear),
                Csv(monster.Sneak),
                Csv(monster.Stealth),

                Csv(monster.CastSpell),
                Csv(monster.CastHeal),
                Csv(monster.CastHarm),
                Csv(monster.CastNova),
                Csv(monster.CastHero),
                Csv(monster.CastIce),
                Csv(monster.CastBlackHole),
                Csv(monster.CastLightning),
                Csv(monster.MagicPower),

                Csv(monster.Catagory),
                Csv(string.Join("|", monster.FriendCatagories)),
                Csv(string.Join("|", monster.EnemyCatagories)),
                Csv(string.Join("|", monster.Friends)),

                Csv(string.Join("|", monster.Treasures)),
                Csv(string.Join("|", monster.TreasureQuantities)),
                Csv(string.Join("|", monster.TreasureChances)),
                Csv(string.Join("|", monster.TreasureData1)),
                Csv(string.Join("|", monster.TreasureData2)),
                Csv(string.Join("|", monster.TreasureData3)),
                Csv(string.Join("|", monster.TreasureData4)),
                Csv(string.Join("|", monster.TreasureTotalUses)),
                Csv(string.Join("|", monster.TreasureTexts)),

                Csv(string.Join("|", monster.TradeGroups)),
                Csv(string.Join("|", monster.TradeGroupSellMaximums)),
                Csv(monster.TradeBuyValue),
                Csv(monster.TradeSellValue),
                Csv(monster.TradeTalkFarewell),
                Csv(monster.TradeTalkSuccess),

                Csv(monster.TalkGreeting),
                Csv(monster.TalkIdle),
                Csv(monster.GreetingAnimation),

                Csv(monster.DeadItem),
                Csv(monster.ImageType),
                Csv(monster.Scan),
                Csv(monster.Roam),
                Csv(monster.RoamChance),
                Csv(monster.KeepDistance),
                Csv(monster.ChaseRange),
                Csv(monster.ChaseItem),
                Csv(monster.MoveSpeed),
                Csv(monster.SightRange),

                Csv(monster.SpawnItem),
                Csv(monster.SpawnItemChance),
                Csv(monster.SpawnItemTimeout),
                Csv(monster.GrowthMonster),
                Csv(monster.GrowthMonsterChance),
                Csv(monster.GrowthMonsterTimeout),
                Csv(monster.IdleTransformItem),
                Csv(monster.ItemTrail),

                Csv(string.Join("|", monster.QuestTakeItems)),
                Csv(string.Join("|", monster.QuestTakeQuantities)),
                Csv(string.Join("|", monster.QuestTalkEntries)),
                Csv(string.Join("|", monster.QuestGiveItems)),
                Csv(string.Join("|", monster.QuestGiveQuantities)),
                Csv(string.Join("|", monster.QuestGiveExperience)),
                Csv(string.Join("|", monster.QuestGiveTames)),
                Csv(string.Join("|", monster.QuestGiveData1)),
                Csv(string.Join("|", monster.QuestGiveData2)),
                Csv(string.Join("|", monster.QuestGiveData3)),
                Csv(string.Join("|", monster.QuestGiveData4))));
        }

        Console.WriteLine("Monster CSV dump complete.");
        Console.WriteLine($"Input:          {inputPath}");
        Console.WriteLine($"Output:         {outputPath}");
        Console.WriteLine($"Monsters dumped:{result.Monsters.Count}");
        Console.WriteLine($"Unknowns:       {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:         {result.Issues.Count}");

        return 0;
    }
    private static int RunMonsters(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing monster.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo monsters \"C:\\Path\\To\\ServerFolder\\monster.ini\"");
            return 1;
        }

        string path = args[1];

        MonsterIniReadResult result = MonsterIniReader.ReadFile(path);

        int totalFlags = result.Monsters.Sum(monster => monster.Flags.Count);
        int monstersWithNames = result.Monsters.Count(monster => !string.IsNullOrWhiteSpace(monster.Name));
        int unnamedMonsters = result.Monsters.Count - monstersWithNames;

        Console.WriteLine("Monster INI Read Result");
        Console.WriteLine("-----------------------");
        Console.WriteLine($"File:                {path}");
        Console.WriteLine($"Monsters loaded:     {result.Monsters.Count}");
        Console.WriteLine($"Monsters with names: {monstersWithNames}");
        Console.WriteLine($"Unnamed monsters:    {unnamedMonsters}");
        Console.WriteLine($"Global fields:       {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:        {result.GlobalFlags.Count}");
        Console.WriteLine($"Monster flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:      {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:        {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        Console.WriteLine("First monsters");
        Console.WriteLine("--------------");

        foreach (var monster in result.Monsters.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(monster.Name)
                ? "(unnamed)"
                : monster.Name;

            string flags = monster.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", monster.Flags.Take(5))}";

            Console.WriteLine($"{monster.Id,6}  {name}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Monsters
            .SelectMany(monster => monster.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Monsters
            .SelectMany(monster => monster.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top monster flags");
            Console.WriteLine("-----------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }

    private static int RunItemRemove(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Missing item.ini path, item ID, or field name.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo itemremove \"C:\\Path\\To\\item.ini\" 100 Food");
            Console.WriteLine("  rpgwo itemremove \"C:\\Path\\To\\item.ini\" 100 Food \"C:\\Path\\To\\item.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  item.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int itemId))
        {
            Console.WriteLine($"Invalid item ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];

        string outputPath = args.Length >= 5
            ? args[4]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool removed = ItemIniWriter.RemoveItemField(
            parseResult.Document,
            itemId,
            fieldName);

        if (!removed)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Item field was not removed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find field '{fieldName}' inside Item={itemId}.");
            Console.WriteLine($"Input: {inputPath}");
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        ItemIniReadResult verifyResult = ItemIniReader.ReadFile(outputPath);

        Console.WriteLine("Item field removed.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Item:         {itemId}");
        Console.WriteLine($"Field:        {fieldName}");
        Console.WriteLine($"Verify items: {verifyResult.Items.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }
    private static bool TryParseCliBool(string text, out bool value)
    {
        value = false;

        if (bool.TryParse(text, out bool parsedBool))
        {
            value = parsedBool;
            return true;
        }

        if (int.TryParse(text, out int parsedInt))
        {
            value = parsedInt != 0;
            return true;
        }

        if (text.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("y", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("on", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("enable", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("enabled", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (text.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("n", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("off", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("disable", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("disabled", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        return false;
    }
    private static int RunItemSet(string[] args)
    {
        if (args.Length < 5)
        {
            Console.WriteLine("Missing item.ini path, item ID, field name, or value.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo itemset \"C:\\Path\\To\\item.ini\" 100 Name \"Better Dandelion Seeds\"");
            Console.WriteLine("  rpgwo itemset \"C:\\Path\\To\\item.ini\" 100 Name \"Better Dandelion Seeds\" \"C:\\Path\\To\\item.work.ini\"");
            Console.WriteLine();
            Console.WriteLine("If no output path is provided, this writes beside the input file as:");
            Console.WriteLine("  item.updated.ini");
            return 1;
        }

        string inputPath = args[1];

        if (!int.TryParse(args[2], out int itemId))
        {
            Console.WriteLine($"Invalid item ID: {args[2]}");
            return 1;
        }

        string fieldName = args[3];
        string value = args[4];

        string outputPath = args.Length >= 6
            ? args[5]
            : BuildUpdatedOutputPath(inputPath);

        IniParseResult parseResult = IniParser.ParseFile(inputPath);

        bool updated = ItemIniWriter.SetItemField(
            parseResult.Document,
            itemId,
            fieldName,
            value);

        if (!updated)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Item update failed.");
            Console.ResetColor();
            Console.WriteLine($"Could not find Item={itemId} in:");
            Console.WriteLine(inputPath);
            return 1;
        }

        IniWriter.WriteFile(outputPath, parseResult.Document);

        ItemIniReadResult verifyResult = ItemIniReader.ReadFile(outputPath);

        Console.WriteLine("Item field updated.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Item:         {itemId}");
        Console.WriteLine($"Field:        {fieldName}");
        Console.WriteLine($"Value:        {value}");
        Console.WriteLine($"Verify items: {verifyResult.Items.Count}");
        Console.WriteLine($"Verify issues:{verifyResult.Issues.Count}");

        if (verifyResult.Issues.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: output file was written, but verification found parse issues.");
            Console.ResetColor();
            return 2;
        }

        return 0;
    }
    private static string BuildUpdatedOutputPath(string inputPath)
    {
        string? folder = Path.GetDirectoryName(inputPath);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputPath);
        string extension = Path.GetExtension(inputPath);

        return Path.Combine(
            folder ?? "",
            $"{fileNameWithoutExtension}.updated{extension}");
    }
    private static int RunItemDump(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Missing item.ini input path or CSV output path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo itemdump \"C:\\Path\\To\\item.ini\" \"C:\\Path\\To\\items.csv\"");
            return 1;
        }

        string inputPath = args[1];
        string outputPath = args[2];

        ItemIniReadResult result = ItemIniReader.ReadFile(inputPath);

        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);

        writer.WriteLine(string.Join(",",
            "Id",
            "Name",
            "Class",
            "Type",
            "SubType",
            "Image",
            "Animation",
            "Animation1",
            "Animation2",
            "Animation3",
            "WearImage",
            "Burden",
            "Value",
            "Group",
            "Size",
            "Stackable",
            "Flags",
            "UnknownFieldCount",
            "IssueCount",
            "DamageLow",
            "DamageHigh",
            "AttackSpeed",
            "CombatSkill",
            "WeaponDamageType",
            "WeaponMinRange",
            "WeaponMaxRange",
            "Ammo",
            "MissleWeapon",
            "ArmorLevel",
            "WeaponArmorLevel",
            "MagicArmorLevel",
            "FireArmorLevel",
            "ElectricArmorLevel",
            "ColdArmorLevel",
            "ThrustArmorLevel",
            "BashArmorLevel",
            "CutArmorLevel",
            "Food",
            "FoodStamina",
            "FoodLife",
            "FoodMana",
            "GrowthItem",
            "GrowthDelta",
            "GrowthDeathChance",
            "GrowthSproutItem",
            "GrowthSproutChance",
            "GrowthSproutRadius",
            "GrowthElevationRange",
            "GrowthLowElevation",
            "GrowthHighElevation",
            "BreakId",
            "BreakDurability",
            "DegradeItem",
            "DegradeDelta",
            "TotalUses",
            "TraderMax",
            "Rarity",
            "Light",
            "Terrain",
            "Build",
            "DungeonEntries"));

        foreach (var item in result.Items.OrderBy(item => item.Id))
        {
            writer.WriteLine(string.Join(",",
                Csv(item.Id),
                Csv(item.Name),
                Csv(item.Class),
                Csv(item.Type),
                Csv(item.SubType),
                Csv(item.Image),
                Csv(item.Animation),
                Csv(item.Animation1),
                Csv(item.Animation2),
                Csv(item.Animation3),
                Csv(item.WearImage),
                Csv(item.Burden),
                Csv(item.Value),
                Csv(item.Group),
                Csv(item.Size),
                Csv(item.Stackable),
                Csv(string.Join("|", item.Flags)),
                Csv(item.UnknownFields.Count),
                Csv(item.Issues.Count),
                Csv(item.DamageLow),
                Csv(item.DamageHigh),
                Csv(item.AttackSpeed),
                Csv(item.CombatSkill),
                Csv(item.WeaponDamageType),
                Csv(item.WeaponMinRange),
                Csv(item.WeaponMaxRange),
                Csv(item.Ammo),
                Csv(item.MissleWeapon),
                Csv(item.ArmorLevel),
                Csv(item.WeaponArmorLevel),
                Csv(item.MagicArmorLevel),
                Csv(item.FireArmorLevel),
                Csv(item.ElectricArmorLevel),
                Csv(item.ColdArmorLevel),
                Csv(item.ThrustArmorLevel),
                Csv(item.BashArmorLevel),
                Csv(item.CutArmorLevel),
                Csv(item.Food),
                Csv(item.FoodStamina),
                Csv(item.FoodLife),
                Csv(item.FoodMana),
                Csv(item.GrowthItem),
                Csv(item.GrowthDelta),
                Csv(item.GrowthDeathChance),
                Csv(item.GrowthSproutItem),
                Csv(item.GrowthSproutChance),
                Csv(item.GrowthSproutRadius),
                Csv(item.GrowthElevationRange),
                Csv(item.GrowthLowElevation),
                Csv(item.GrowthHighElevation),
                Csv(item.BreakId),
                Csv(item.BreakDurability),
                Csv(item.DegradeItem),
                Csv(item.DegradeDelta),
                Csv(item.TotalUses),
                Csv(item.TraderMax),
                Csv(item.Rarity),
                Csv(item.Light),
                Csv(item.Terrain),
                Csv(item.Build),
                Csv(string.Join("|", item.DungeonEntries))));
        }

        Console.WriteLine("Item CSV dump complete.");
        Console.WriteLine($"Input:        {inputPath}");
        Console.WriteLine($"Output:       {outputPath}");
        Console.WriteLine($"Items dumped: {result.Items.Count}");
        Console.WriteLine($"Unknowns:     {result.UnknownFieldCount}");
        Console.WriteLine($"Issues:       {result.Issues.Count}");

        return 0;
    }
    private static string Csv(string? value)
    {
        value ??= "";

        bool mustQuote =
            value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\n') ||
            value.Contains('\r');

        if (!mustQuote)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Csv(int? value)
    {
        return value?.ToString() ?? "";
    }

    private static string Csv(double? value)
    {
        return value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    }

    private static string Csv(bool? value)
    {
        return value?.ToString() ?? "";
    }
    private static int RunItems(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing item.ini file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo items \"C:\\Path\\To\\ServerFolder\\item.ini\"");
            return 1;
        }

        string path = args[1];

        ItemIniReadResult result = ItemIniReader.ReadFile(path);

        int totalFlags = result.Items.Sum(item => item.Flags.Count);
        int itemsWithNames = result.Items.Count(item => !string.IsNullOrWhiteSpace(item.Name));
        int unnamedItems = result.Items.Count - itemsWithNames;

        Console.WriteLine("Item INI Read Result");
        Console.WriteLine("--------------------");
        Console.WriteLine($"File:             {path}");
        Console.WriteLine($"Items loaded:     {result.Items.Count}");
        Console.WriteLine($"Items with names: {itemsWithNames}");
        Console.WriteLine($"Unnamed items:    {unnamedItems}");
        Console.WriteLine($"Global fields:    {result.GlobalFields.Count}");
        Console.WriteLine($"Global flags:     {result.GlobalFlags.Count}");
        Console.WriteLine($"Item flags:       {totalFlags}");
        Console.WriteLine($"Unknown fields:   {result.UnknownFieldCount}");
        Console.WriteLine($"Parse issues:     {result.Issues.Count}");
        Console.WriteLine();

        if (result.GlobalFields.Count > 0)
        {
            Console.WriteLine("Global fields");
            Console.WriteLine("-------------");

            foreach (var field in result.GlobalFields.Take(25))
                Console.WriteLine($"{field.Key}={field.Value}");

            if (result.GlobalFields.Count > 25)
                Console.WriteLine($"...and {result.GlobalFields.Count - 25} more global fields.");

            Console.WriteLine();
        }

        Console.WriteLine("First items");
        Console.WriteLine("-----------");

        foreach (var item in result.Items.Take(25))
        {
            string name = string.IsNullOrWhiteSpace(item.Name)
                ? "(unnamed)"
                : item.Name;

            string flags = item.Flags.Count == 0
                ? ""
                : $" Flags: {string.Join(", ", item.Flags.Take(5))}";

            Console.WriteLine($"{item.Id,6}  {name}{flags}");
        }

        if (result.HasIssues)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Issues");
            Console.WriteLine("------");
            Console.ResetColor();

            foreach (var issue in result.Issues.Take(25))
                Console.WriteLine(issue);

            if (result.Issues.Count > 25)
                Console.WriteLine($"...and {result.Issues.Count - 25} more issues.");
        }

        var topUnknownFields = result.Items
            .SelectMany(item => item.UnknownFields)
            .GroupBy(field => field.Key, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topUnknownFields.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top unknown fields");
            Console.WriteLine("------------------");

            foreach (var group in topUnknownFields)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        var topFlags = result.Items
            .SelectMany(item => item.Flags)
            .GroupBy(flag => flag, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (topFlags.Any())
        {
            Console.WriteLine();
            Console.WriteLine("Top item flags");
            Console.WriteLine("--------------");

            foreach (var group in topFlags)
                Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        return 0;
    }
    private static int RunHelp()
    {
        PrintHelp();
        return 0;
    }

    private static int RunUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown command: {command}");
        Console.ResetColor();
        Console.WriteLine();

        PrintHelp();
        return 1;
    }

    private static int RunScan(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing folder path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo scan \"C:\\Path\\To\\Server\"");
            return 1;
        }

        string rootPath = args[1];

        ServerProject project = ServerFolderScanner.Scan(rootPath);
        var summary = new ServerProjectSummary(project);

        Console.WriteLine("RPGWO Server Folder Scan");
        Console.WriteLine("------------------------");
        Console.WriteLine($"Root: {summary.RootPath}");
        Console.WriteLine();

        Console.WriteLine("Summary");
        Console.WriteLine("-------");
        Console.WriteLine($"Total files/folders:       {summary.TotalFiles}");
        Console.WriteLine($"Definition files:          {summary.DefinitionFileCount}");
        Console.WriteLine($"Runtime data files:        {summary.RuntimeDataFileCount}");
        Console.WriteLine($"Sprite sheets:             {summary.SpriteSheetCount}");
        Console.WriteLine($"Unknown files/folders:     {summary.UnknownFileCount}");
        Console.WriteLine();

        Console.WriteLine("Detected definition files");
        Console.WriteLine("-------------------------");

        PrintDetectedFile(project.WorldIni, "world.ini");
        PrintDetectedFile(project.ItemIni, "item.ini");
        PrintDetectedFile(project.MonsterIni, "monster.ini");
        PrintDetectedFile(project.MagicIni, "magic.ini");
        PrintDetectedFile(project.SkillIni, "skill.ini");
        PrintDetectedFile(project.ItemUseIni, "itemuse.ini");
        PrintDetectedFile(project.MultiUseIni, "multiuse.ini");
        PrintDetectedFile(project.TreasureIni, "treasure.ini");
        PrintDetectedFile(project.AnimationIni, "animation.ini");
        PrintDetectedFile(project.WaterSideIni, "waterside.ini");
        PrintDetectedFile(project.UnderSideIni, "underside.ini");

        Console.WriteLine();
        Console.WriteLine("All recognized files");
        Console.WriteLine("--------------------");

        foreach (ServerFileReference file in project.Files
                     .Where(file => file.Kind != ServerFileKind.Unknown)
                     .OrderBy(file => file.Kind)
                     .ThenBy(file => file.RelativePath))
        {
            Console.WriteLine($"{file.Kind,-16} {file.RelativePath}");
        }

        return 0;
    }

    private static void PrintDetectedFile(ServerFileReference? file, string label)
    {
        if (file is null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[missing] {label}");
            Console.ResetColor();
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[found]   ");
        Console.ResetColor();
        Console.WriteLine(file.RelativePath);
    }

    private static int RunParse(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing INI file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo parse \"C:\\Path\\To\\Server\\item.ini\"");
            return 1;
        }

        string path = args[1];

        IniParseResult result = IniParser.ParseFile(path);
        IniDocument document = result.Document;

        Console.WriteLine("INI Parse Result");
        Console.WriteLine("----------------");
        Console.WriteLine($"File:          {path}");
        Console.WriteLine($"Lines:         {document.LineCount}");
        Console.WriteLine($"Key/value:     {document.KeyValueCount}");
        Console.WriteLine($"Comments:      {document.CommentCount}");
        Console.WriteLine($"Blank:         {document.BlankCount}");
        Console.WriteLine($"Sections:      {document.SectionCount}");
        Console.WriteLine($"Raw/unknown:   {document.RawCount}");
        Console.WriteLine($"Unique keys:   {document.UniqueKeys.Count}");
        Console.WriteLine();

        if (result.HasWarnings)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warnings");
            Console.WriteLine("--------");
            Console.ResetColor();

            foreach (string warning in result.Warnings)
                Console.WriteLine(warning);

            Console.WriteLine();
        }

        Console.WriteLine("Top keys");
        Console.WriteLine("--------");

        foreach (var group in document.KeyValueLines
                     .GroupBy(line => line.Key, StringComparer.OrdinalIgnoreCase)
                     .OrderByDescending(group => group.Count())
                     .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                     .Take(25))
        {
            Console.WriteLine($"{group.Key,-30} {group.Count()}");
        }

        if (document.RawCount > 0)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Raw/unknown lines");
            Console.WriteLine("-----------------");
            Console.ResetColor();

            foreach (IniRawLine raw in document.RawLines.Take(25))
                Console.WriteLine($"Line {raw.LineNumber}: {raw.OriginalText}");

            if (document.RawCount > 25)
                Console.WriteLine($"...and {document.RawCount - 25} more raw lines.");
        }

        return 0;
    }

    private static int RunRoundtrip(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Missing INI file path.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  rpgwo roundtrip \"C:\\Path\\To\\Server\\item.ini\"");
            return 1;
        }

        string path = args[1];

        if (!File.Exists(path))
            throw new FileNotFoundException("INI file was not found.", path);

        string originalText = File.ReadAllText(path);

        IniParseResult result = IniParser.ParseFile(path);
        string outputText = IniWriter.WriteToString(result.Document);

        string outputPath = BuildRoundtripOutputPath(path);
        File.WriteAllText(outputPath, outputText);

        bool exactMatch = string.Equals(originalText, outputText, StringComparison.Ordinal);

        Console.WriteLine("Roundtrip Result");
        Console.WriteLine("----------------");
        Console.WriteLine($"Original:    {path}");
        Console.WriteLine($"Output:      {outputPath}");
        Console.WriteLine($"Exact match: {exactMatch}");

        if (!exactMatch)
        {
            Console.WriteLine();

            DifferenceInfo? difference = FindFirstDifference(originalText, outputText);

            if (difference is not null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("First difference");
                Console.WriteLine("----------------");
                Console.ResetColor();

                Console.WriteLine($"Line:     {difference.LineNumber}");
                Console.WriteLine($"Column:   {difference.ColumnNumber}");
                Console.WriteLine($"Original: {difference.OriginalLine}");
                Console.WriteLine($"Output:   {difference.OutputLine}");
            }
        }

        return exactMatch ? 0 : 2;
    }

    private static string BuildRoundtripOutputPath(string path)
    {
        string? folder = Path.GetDirectoryName(path);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        return Path.Combine(
            folder ?? "",
            $"{fileNameWithoutExtension}.roundtrip{extension}");
    }

    private static DifferenceInfo? FindFirstDifference(string original, string output)
    {
        string[] originalLines = NormalizeNewLines(original).Split('\n');
        string[] outputLines = NormalizeNewLines(output).Split('\n');

        int maxLineCount = Math.Max(originalLines.Length, outputLines.Length);

        for (int lineIndex = 0; lineIndex < maxLineCount; lineIndex++)
        {
            string originalLine = lineIndex < originalLines.Length
                ? originalLines[lineIndex]
                : "";

            string outputLine = lineIndex < outputLines.Length
                ? outputLines[lineIndex]
                : "";

            if (string.Equals(originalLine, outputLine, StringComparison.Ordinal))
                continue;

            int column = FindFirstColumnDifference(originalLine, outputLine);

            return new DifferenceInfo(
                LineNumber: lineIndex + 1,
                ColumnNumber: column + 1,
                OriginalLine: originalLine,
                OutputLine: outputLine);
        }

        return null;
    }

    private static int FindFirstColumnDifference(string originalLine, string outputLine)
    {
        int maxLength = Math.Max(originalLine.Length, outputLine.Length);

        for (int index = 0; index < maxLength; index++)
        {
            char originalChar = index < originalLine.Length
                ? originalLine[index]
                : '\0';

            char outputChar = index < outputLine.Length
                ? outputLine[index]
                : '\0';

            if (originalChar != outputChar)
                return index;
        }

        return 0;
    }

    private static string NormalizeNewLines(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  scan       Detect and classify files in a server folder.");
        Console.WriteLine("  parse      Parse an INI file and print basic counts.");
        Console.WriteLine("  roundtrip  Parse an INI file, write it back, and compare output.");
        Console.WriteLine("  items      Read item.ini into typed item definitions.");
        Console.WriteLine("  help       Show this help text.");
        Console.WriteLine("  rpgwo itemdump  \"C:\\Path\\To\\ServerFolder\\item.ini\" \"C:\\Path\\To\\items.csv\"");
        Console.WriteLine("  itemdump   Export typed item.ini data to CSV.");
        Console.WriteLine("  itemset    Safely update one item field and write item.updated.ini.");
        Console.WriteLine("  itemflag   Safely add/remove one bare item flag and write item.updated.ini.");
        Console.WriteLine("  itemremove Safely remove one item field, save, and verify output.");
        Console.WriteLine("  monsters   Read monster.ini into typed monster definitions.");
        Console.WriteLine("  monsterdump Export typed monster.ini data to CSV.");
        Console.WriteLine("  monsterset Safely update one monster field, save, and verify output.");
        Console.WriteLine("  monsterflag Safely add/remove one bare monster flag, save, and verify output.");
        Console.WriteLine("  monsterremove Safely remove one monster field, save, and verify output.");
        Console.WriteLine("  skills     Read skill.ini into typed skill definitions.");
        Console.WriteLine("  skilldump  Export typed skill.ini data to CSV.");
        Console.WriteLine("  skillset   Safely update one skill field, save, and verify output.");
        Console.WriteLine("  skillflag  Safely add/remove one bare skill flag, save, and verify output.");
        Console.WriteLine("  skillremove Safely remove one skill field, save, and verify output.");
        Console.WriteLine("  usages     Read itemuse.ini into typed usage definitions.");
        Console.WriteLine("  usagedump  Export typed itemuse.ini data to CSV.");
        Console.WriteLine("  usageset    Safely update one itemuse field, save, and verify output.");
        Console.WriteLine("  usageflag   Safely add/remove one bare itemuse flag, save, and verify output.");
        Console.WriteLine("  usageremove Safely remove one itemuse field, save, and verify output.");
        Console.WriteLine("  multiuses     Read multiuse.ini into typed recipe definitions.");
        Console.WriteLine("  multiusedump  Export typed multiuse.ini data to CSV.");
        Console.WriteLine("  multiuseset    Safely update one multiuse field, save, and verify output.");
        Console.WriteLine("  multiuseflag   Safely add/remove one bare multiuse flag, save, and verify output.");
        Console.WriteLine("  multiuseremove Safely remove one multiuse field, save, and verify output.");
        Console.WriteLine("  magics     Read magic.ini into typed spell definitions.");
        Console.WriteLine("  magicdump  Export typed magic.ini data to CSV.");
        Console.WriteLine("  magicset    Safely update one spell field, save, and verify output.");
        Console.WriteLine("  magicflag   Safely add/remove one bare spell flag, save, and verify output.");
        Console.WriteLine("  magicremove Safely remove one spell field, save, and verify output.");
        Console.WriteLine("  treasures     Read treasure.ini into typed treasure definitions.");
        Console.WriteLine("  treasuredump  Export typed treasure.ini data to CSV.");
        Console.WriteLine("  treasureset    Safely update one treasure field, save, and verify output.");
        Console.WriteLine("  treasureflag   Safely add/remove one bare treasure flag, save, and verify output.");
        Console.WriteLine("  treasureremove Safely remove one treasure field, save, and verify output.");
        Console.WriteLine("  world      Read world.ini into global world settings.");
        Console.WriteLine("  worlddump  Export world.ini settings and flags to CSV.");
        Console.WriteLine("  worldset    Safely update one world setting, save, and verify output.");
        Console.WriteLine("  worldflag   Safely add/remove one bare world flag, save, and verify output.");
        Console.WriteLine("  worldremove Safely remove all matching world settings, save, and verify output.");
        Console.WriteLine("  animations     Read animation.ini into typed animation definitions.");
        Console.WriteLine("  animationdump  Export typed animation.ini data to CSV.");
        Console.WriteLine("  animationset    Safely update one animation field, save, and verify output.");
        Console.WriteLine("  animationflag   Safely add/remove one bare animation flag, save, and verify output.");
        Console.WriteLine("  animationremove Safely remove animation field lines, save, and verify output.");
    }

    private sealed record DifferenceInfo(
        int LineNumber,
        int ColumnNumber,
        string OriginalLine,
        string OutputLine);
}