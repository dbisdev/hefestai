using Loremaster.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Loremaster.Application.Features.EntityGeneration.Services;

public class TemplateResolutionService : ITemplateResolutionService
{
    private readonly IEntityTemplateRepository _templateRepository;
    private readonly IPromptBuilderService _promptBuilder;
    private readonly ILogger<TemplateResolutionService> _logger;

    public TemplateResolutionService(
        IEntityTemplateRepository templateRepository,
        IPromptBuilderService promptBuilder,
        ILogger<TemplateResolutionService> logger)
    {
        _templateRepository = templateRepository;
        _promptBuilder = promptBuilder;
        _logger = logger;
    }

    public async Task<TemplateResolutionResult> ResolveTemplateAsync(
        Guid? gameSystemId,
        Guid userId,
        string entityTypeName,
        string entityTypeDisplayName,
        CancellationToken cancellationToken = default)
    {
        if (!gameSystemId.HasValue)
            return TemplateResolutionResult.WithoutTemplate(
                GetFallbackExampleJson(entityTypeName),
                GetFallbackFieldDescriptions(entityTypeName));

        var template = await _templateRepository.GetConfirmedTemplateForEntityTypeAsync(
            gameSystemId.Value,
            userId,
            entityTypeName,
            cancellationToken);

        if (template == null)
        {
            _logger.LogDebug(
                "No confirmed template found for entity type '{EntityType}' in game system {GameSystemId}",
                entityTypeName, gameSystemId);

            return TemplateResolutionResult.WithoutTemplate(
                GetFallbackExampleJson(entityTypeName),
                GetFallbackFieldDescriptions(entityTypeName));
        }

        _logger.LogDebug(
            "Found confirmed template {TemplateId} for entity type '{EntityType}' in game system {GameSystemId}",
            template.Id, entityTypeName, gameSystemId);

        var (exampleJson, fieldDescriptions) = _promptBuilder.BuildTemplateSchema(template, entityTypeDisplayName);

        return TemplateResolutionResult.WithTemplate(
            template,
            exampleJson,
            fieldDescriptions,
            GetFallbackExampleJson(entityTypeName),
            GetFallbackFieldDescriptions(entityTypeName));
    }

    private static string GetFallbackExampleJson(string entityType) => entityType.ToLowerInvariant() switch
    {
        "character" => @"{""name"":""Zephyr-9"",""description"":""A rogue android..."",""stats"":{
    ""STRENGTH"": 3,
    ""AGILITY"": 4,
    ""WITS"": 4,
    ""EMPATHY"": 5,
    ""SKILLS"": {
      ""MOBILITY"": 2,
      ""OBSERVATION"": 4,
      ""MEDICAL AID"": 4
    },
    ""TALENT"": ""Field Medic"",
    ""GEAR"": ""Medkit, Surgical kit, Four doses of Naproleve"",
    ""CASH"": 1000}}",
        "solar_system" or "solar-system" => @"{""name"":""Nexus Prime"",""description"":""A binary system..."",""stats"":{""planets"": 
                [{
    ""orbital_position"": 1,
    ""type"": ""Terrestrial"",
    ""name"": ""LV-426"",        
    ""size"": 4000,        
    ""gravity"": 0.2,        
    ""atmosphere"": ""Thin, unbreathable"",        
    ""temperature"": ""Hot"",        
    ""features"": ""Barren, cratered surface"",        
    ""resources"": ""Trace amounts of common metals""
  }]}}",
        "vehicle" => @"{""name"":""Phantom-X7"",""description"":""A stealth interceptor..."",""stats"":{""SPEED"":95,""ARMOR"":40,""CARGO"":20}}",
        "npc" => @"{""name"":""Vex Morrow"",""description"":""Former corporate spy turned information broker..."",""stats"":{""occupation"":""Information Broker"",""personality"":""Cunning but fair"",""CHA"":85,""INT"":75,""WIS"":60}}",
        "enemy" => @"{""name"":""Void Stalker"",""description"":""An alien predator with cloaking abilities..."",""stats"":{""species"":""Alien Predator"",""threatLevel"":""dangerous"",""abilities"":""Cloaking field, venomous claws"",""weakness"":""Sensitive to bright light"",""HP"":150,""ATK"":75,""DEF"":40,""SPD"":90}}",
        "mission" => @"{""name"":""Operation Blackout"",""description"":""Corporate forces have seized the research facility..."",""stats"":{""objective"":""Infiltrate the facility and retrieve the data core"",""rewards"":""5000 credits + reputation boost"",""difficulty"":""HARD"",""estimatedDuration"":""3-4 hours""}}",
        "encounter" => @"{""name"":""Cargo Bay Showdown"",""description"":""The party enters the cargo bay to find hostile forces..."",""stats"":{""environment"":""Large open space with shipping containers providing cover"",""participants"":[""Security Droid x2"",""Corporate Enforcer""],""difficulty"":""MEDIUM"",""loot"":""Prototype weapon, 1200 credits""}}",
        _ => @"{""name"":""<Name>"",""description"":""<Description>"",""stats"":{}}"
    };

    private static string GetFallbackFieldDescriptions(string entityType) => entityType.ToLowerInvariant() switch
    {
        "character" => @"- name: A unique character name fitting the setting
- description: A 2-3 paragraphs backstory based on the lore
- stats: An object with the attributes required by the character creation rules",
        "solar_system" or "solar-system" => @"- name: A unique star system name fitting the setting
- description: A 2-3 paragraph description
- stats: An object with planets array and other system data",
        "vehicle" => @"- name: A unique vehicle designation/name fitting the setting
- description: A 2-3 sentence description of capabilities
- stats: An object with SPEED (1-100), ARMOR (1-100), CARGO (1-100)",
        "npc" => @"- name: A unique sci-fi name fitting their species and the setting's naming conventions
- description: A 2-3 sentence backstory explaining who they are and their motivations
- stats: An object with occupation, personality, and attributes (CHA, INT, WIS from 1-100)",
        "enemy" => @"- name: A threatening designation or creature name fitting the setting
- description: A 2-3 sentence description of the creature and its abilities
- stats: An object with species, threatLevel, abilities, weakness, HP (50-500), ATK (1-100), DEF (1-100), SPD (1-100)",
        "mission" => @"- name: A code name or operation title (e.g., ""Operation Silent Dawn"") fitting the setting
- description: A 2-3 sentence mission briefing explaining the situation
- stats: An object with objective, rewards, difficulty, and estimatedDuration",
        "encounter" => @"- name: An evocative encounter name (e.g., ""Ambush at Sector 7"") fitting the setting
- description: A 2-3 sentence description of the situation and how the encounter begins
- stats: An object with environment, participants array, difficulty, and loot",
        _ => @"- name: A unique name fitting the setting
- description: A description
- stats: An object with relevant attributes"
    };
}
