using CampusCare.Core.DTOs;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Services
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AIService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<AIAnalysisResult> AnalyzeComplaintAsync(string title, string description, string location)
        {
            string apiKey = _configuration["AISettings:ApiKey"] ?? string.Empty;
            string apiEndpoint = _configuration["AISettings:Endpoint"] ?? string.Empty;

            // If external AI key is provided, attempt external LLM integration
            if (!string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiEndpoint))
            {
                try
                {
                    // Call external AI API (e.g. Gemini / OpenAI compatible JSON endpoint)
                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new
                            {
                                parts=new[]
                                {
                                    new
                                    {
                                         text =
                                            "You are an AI complaint classification system for a college campus.\n\n" +

                                            "Analyze the following complaint:\n" +
                                            "Title: " + title + "\n" +
                                            "Description: " + description + "\n" +
                                            "Location: " + location + "\n\n" +

                                            "Choose EXACTLY ONE Category from this list:\n" +
                                            "IT / Wi-Fi\n" +
                                            "Classroom\n" +
                                            "Laboratory\n" +
                                            "Hostel\n" +
                                            "Library\n" +
                                            "Maintenance\n" +
                                            "Cleanliness\n" +
                                            "Transportation\n" +
                                            "Security\n" +
                                            "Other\n\n" +

                                            "Choose EXACTLY ONE Department from this list:\n" +
                                            "Information Technology\n" +
                                            "Facility Maintenance\n" +
                                            "Hostel Administration\n" +
                                            "Library Services\n" +
                                            "Campus Security\n" +
                                            "Transport & Fleet\n" +
                                            "General Administration\n\n" +

                                            "Priority must be exactly one of:\n" +
                                            "Low\n" +
                                            "Medium\n" +
                                            "High\n" +
                                            "Critical\n\n" +

                                            "Return ONLY valid JSON. Do not include markdown, explanations, or code fences.\n\n" +

                                            "Required JSON format:\n" +
                                            "\"Category\":\"Other\",\"Department\":\"General Administration\",\"Priority\":\"Medium\",\"Summary\":\"Short summary of complaint\"}"
                
                                    }
                                }
                            }
                    }
                    };

                    // We wrap with timeout so AI failure NEVER breaks complaint submission
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json");
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    var response = await _httpClient.PostAsync(apiEndpoint, content, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        return new AIAnalysisResult
                        {
                            Category = root.TryGetProperty("Category", out var c) ? c.GetString() ?? "Other" : "Other",
                            Department = root.TryGetProperty("Department", out var d) ? d.GetString() ?? "Information Technology" : "Information Technology",
                            Priority = Enum.TryParse<PriorityLevel>(root.TryGetProperty("Priority", out var p) ? p.GetString() : "Medium", out var parsedP) ? parsedP : PriorityLevel.Medium,
                            Summary = root.TryGetProperty("Summary", out var s) ? s.GetString() ?? title : title,
                            IsSuccess = true,
                            ModelUsed = _configuration["AISettings:Model"] ?? "External-LLM"
                        };
                    }
                }
                catch (Exception ex)
                {
                    // Fallback on exception (e.g., timeout/offline)
                    Console.WriteLine($"[AIService Warning] External AI unavailable: {ex.Message}. Falling back to Rule Engine.");

                }
            }

            // Local Rule-Based Fallback Engine (Runs when AI API key missing or offline)
            return GenerateFallbackAnalysis(title, description, location);
        }

        private AIAnalysisResult GenerateFallbackAnalysis(string title, string description, string location)
        {
            string text = $"{title} {description} {location}".ToLowerInvariant();

            string category = "Other";
            string department = "General Administration";
            PriorityLevel priority = PriorityLevel.Medium;

            // 1. Information Technology
            if (text.Contains("wifi") || text.Contains("wi-fi") || text.Contains("internet") || text.Contains("network") || text.Contains("router") || text.Contains("ethernet"))
            {
                category = "IT / Wi-Fi";
                department = "Information Technology";
                priority = (text.Contains("exam") || text.Contains("lab") || text.Contains("down")) ? PriorityLevel.High : PriorityLevel.Medium;
            }
            else if (text.Contains("pc") || text.Contains("computer") || text.Contains("monitor") || text.Contains("software") || text.Contains("printer") || text.Contains("server"))
            {
                category = "Laboratory";
                department = "Information Technology";
                priority = PriorityLevel.High;
            }
            // 2. Maintenance & Plumbing / Electrical / Facilities
            else if (text.Contains("pipe") || text.Contains("plumb") || text.Contains("leak") || text.Contains("tap") || text.Contains("toilet") || text.Contains("flush") || text.Contains("drain"))
            {
                category = "Maintenance";
                department = "Facility Maintenance";
                priority = (text.Contains("leak") || text.Contains("overflow")) ? PriorityLevel.High : PriorityLevel.Medium;
            }
            else if (text.Contains("light") || text.Contains("fan") || text.Contains("a/c") || text.Contains("air condition") || text.Contains("projector") || text.Contains("bench") || text.Contains("board") || text.Contains("desk") || text.Contains("chair"))
            {
                category = "Classroom";
                department = "Facility Maintenance";
                priority = PriorityLevel.Medium;
            }
            // 3. Hostel Administration
            else if (text.Contains("hostel") || text.Contains("mess") || text.Contains("warden") || text.Contains("bed") || text.Contains("mattress") || text.Contains("room allocation"))
            {
                category = "Hostel";
                department = "Hostel Administration";
                priority = text.Contains("food") || text.Contains("water") ? PriorityLevel.High : PriorityLevel.Medium;
            }
            else if (text.Contains("clean") || text.Contains("dirt") || text.Contains("trash") || text.Contains("sanitation") || text.Contains("garbage") || text.Contains("dustbin"))
            {
                category = "Cleanliness";
                department = "Facility Maintenance";
                priority = PriorityLevel.Low;
            }
            // 4. Security
            else if (text.Contains("fire") || text.Contains("hazard") || text.Contains("stolen") || text.Contains("security") || text.Contains("theft") || text.Contains("guard") || text.Contains("gate") || text.Contains("id card") || text.Contains("parking"))
            {
                category = "Security";
                department = "Campus Security";
                priority = (text.Contains("theft") || text.Contains("fire")) ? PriorityLevel.Critical : PriorityLevel.High;
            }
            // 5. Transportation
            else if (text.Contains("bus") || text.Contains("transport") || text.Contains("shuttle") || text.Contains("driver") || text.Contains("vehicle") || text.Contains("route"))
            {
                category = "Transportation";
                department = "Transport & Fleet";
                priority = PriorityLevel.Medium;
            }
            // 6. Library
            else if (text.Contains("library") || text.Contains("book") || text.Contains("journal") || text.Contains("reading room"))
            {
                category = "Library";
                department = "Library Services";
                priority = PriorityLevel.Low;
            }
            // 7. General Admin
            else if (text.Contains("fee") || text.Contains("scholarship") || text.Contains("certificate") || text.Contains("office") || text.Contains("admin"))
            {
                category = "Other";
                department = "General Administration";
                priority = PriorityLevel.Medium;
            }

            string summary = title.Length > 80 ? title.Substring(0, 77) + "..." : title;
            if (!string.IsNullOrWhiteSpace(description))
            {
                summary += $" - {description.Split('.')[0]}";
                if (summary.Length > 150) summary = summary.Substring(0, 147) + "...";
            }

            return new AIAnalysisResult
            {
                Category = category,
                Department = department,
                Priority = priority,
                Summary = summary,
                IsSuccess = true,
                ModelUsed = "CampusCare-RuleEngine-v1"
            };
        }
    }
}
