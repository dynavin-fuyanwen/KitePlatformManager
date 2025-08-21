using System.Text.Json;

namespace KitePlatformManager.Models;

public record GenericModel(Dictionary<string, JsonElement> Fields);
