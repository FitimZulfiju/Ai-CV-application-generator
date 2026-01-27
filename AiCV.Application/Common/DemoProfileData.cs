namespace AiCV.Application.Common;

/// <summary>
/// Static demo profile data for the "View Sample CV" feature.
/// This is a read-only reference that all users can view.
/// </summary>
public static class DemoProfileData
{
    public static CandidateProfile GetSampleProfile() =>
        new()
        {
            FullName = "Test User",
            Title = "Full-Stack .NET Developer",
            Email = "testuser@aicv.com",
            PhoneNumber = "+12 34 56 78 90",
            LinkedInUrl = "https://linkedin.com/in/testuser",
            PortfolioUrl = "https://github.com/testuser",
            Location = "City, Country",
            ProfessionalSummary =
                "Full-Stack .NET Developer with <strong style=\"color:blue;font-weight:normal;\">7+ years</strong> building enterprise web applications and microservices. Specialized in <strong style=\"color:blue;font-weight:normal;\">.NET Core, Blazor, ASP.NET Core,</strong> and cloud-native architectures. Proven track record delivering scalable solutions for transportation, document processing, and ERP systems across consulting and product companies. Strong expertise in API design, system integration, and Agile development.",
            Skills =
            [
                new Skill { Name = ".NET 9/10", Category = "Backend Development" },
                new Skill { Name = "ASP.NET Core", Category = "Backend Development" },
                new Skill { Name = "C#", Category = "Backend Development" },
                new Skill { Name = "Entity Framework Core", Category = "Backend Development" },
                new Skill { Name = "ADO.NET", Category = "Backend Development" },
                new Skill { Name = "Microservices Architecture", Category = "Backend Development" },
                new Skill { Name = "REST APIs", Category = "Backend Development" },
                new Skill { Name = "GraphQL", Category = "Backend Development" },
                new Skill
                {
                    Name = "Blazor (Server/WebAssembly)",
                    Category = "Frontend Development",
                },
                new Skill { Name = "Angular", Category = "Frontend Development" },
                new Skill { Name = "HTML5", Category = "Frontend Development" },
                new Skill { Name = "CSS3", Category = "Frontend Development" },
                new Skill { Name = "JavaScript", Category = "Frontend Development" },
                new Skill { Name = "TypeScript", Category = "Frontend Development" },
                new Skill { Name = "MudBlazor", Category = "Frontend Development" },
                new Skill { Name = "SQL Server", Category = "Database & Data" },
                new Skill { Name = "Azure SQL Database", Category = "Database & Data" },
                new Skill { Name = "Entity Framework Core", Category = "Database & Data" },
                new Skill { Name = "Database Design", Category = "Database & Data" },
                new Skill { Name = "Data Pipelines", Category = "Database & Data" },
                new Skill { Name = "Docker", Category = "DevOps & Tools" },
                new Skill { Name = "Kubernetes", Category = "DevOps & Tools" },
                new Skill { Name = "Azure DevOps", Category = "DevOps & Tools" },
                new Skill { Name = "Git", Category = "DevOps & Tools" },
                new Skill { Name = "GitHub", Category = "DevOps & Tools" },
                new Skill { Name = "CI/CD Pipelines", Category = "DevOps & Tools" },
                new Skill { Name = "Microservices", Category = "Architecture & Patterns" },
                new Skill { Name = "Clean Architecture", Category = "Architecture & Patterns" },
                new Skill { Name = "API Design", Category = "Architecture & Patterns" },
                new Skill { Name = "System Integration", Category = "Architecture & Patterns" },
                new Skill { Name = "Azure", Category = "Cloud & Integration" },
                new Skill { Name = "OAuth 2.0", Category = "Cloud & Integration" },
                new Skill { Name = "SignalR", Category = "Cloud & Integration" },
                new Skill
                {
                    Name = "Third-party API Integration (Visma, DHL, Google Drive, Magento)",
                    Category = "Cloud & Integration",
                },
            ],
            WorkExperience =
            [
                new Experience
                {
                    CompanyName = "Company 1",
                    JobTitle = "Software Engineer / Developer",
                    StartDate = new DateTime(2025, 3, 1),
                    EndDate = new DateTime(2025, 8, 1),
                    IsCurrentRole = false,
                    Location = "City, State",
                    Description =
                        "Developed and maintained <strong style=\"color:blue;font-weight:normal;\">25+ Blazor components</strong> for enterprise transportation system supporting Nordic logistics operations.\n"
                        + "Integrated with <strong style=\"color:blue;font-weight:normal;\">1500+ REST APIs endpoints,</strong> implementing 12 new features during 6-month tenure.\n"
                        + "Optimized frontend performance, reducing load times by <strong style=\"color:blue;font-weight:normal;\">30%</strong> through lazy loading and component caching.\n"
                        + "Fixed <strong style=\"color:blue;font-weight:normal;\">35+ critical bugs</strong> in legacy codebase while maintaining 100% backward compatibility.\n"
                        + "Collaborated in Agile Scrum team (8 developers, 3 testers, 2 product owners) using Azure DevOps for sprint planning, code reviews, and CI/CD pipelines.",
                },
                new Experience
                {
                    CompanyName = "Company 2",
                    JobTitle = "Software Engineer",
                    StartDate = new DateTime(2023, 7, 1),
                    EndDate = new DateTime(2024, 7, 1),
                    IsCurrentRole = false,
                    Location = "City, State",
                    Description =
                        "Architected microservices-based Intelligent Document Processing system, processing <strong style=\"color:blue;font-weight:normal;\">10,000+ documents monthly</strong> with 95% accuracy.\n"
                        + "Built full-stack web application using Blazor and GraphQL, reducing API response time by <strong style=\"color:blue;font-weight:normal;\">60%</strong> compared to traditional REST.\n"
                        + "Implemented OCR and AI-powered document classification, automating workflows and reducing manual processing time by <strong style=\"color:blue;font-weight:normal;\">70%.</strong>\n"
                        + "Designed and developed <strong style=\"color:blue;\">15+ RESTful APIs and GraphQL</strong> endpoints for seamless frontend-backend communication.\n"
                        + "Collaborated with cross-functional Agile team (5 developers, 2 QA engineers) delivering features in 2-week sprints.\n"
                        + "Optimized security implementing OAuth 2.0, JWT authentication, and role-based access control.",
                },
                new Experience
                {
                    CompanyName = "Company 3",
                    JobTitle = "Software Developer",
                    StartDate = new DateTime(2022, 6, 1),
                    EndDate = new DateTime(2023, 6, 1),
                    IsCurrentRole = false,
                    Location = "City, State",
                    Description =
                        "Engineered Visma Business Solutions integrations for <strong style=\"color:blue;\">8 clients,</strong> automating invoicing and inventory management, saving <strong style=\"color:blue;font-weight:normal;\">100+ hours/month</strong> per client.\n"
                        + "Developed Magento-Visma e-commerce connector synchronizing <strong style=\"color:blue;font-weight:normal;\">5,000 products</strong> in real time with bi-directional data flow.\n"
                        + "Built automated PDF report generator extracting Visma data, processing images, and emailing reports on a scheduled basis.\n"
                        + "Implemented DHL Express booking system with address validation API and automated shipping label generation integrated with Visma.\n"
                        + "Independently managed 4-6 concurrent client projects, delivering solutions on time with minimal supervision.\n"
                        + "Gained deep expertise in REST design, third-party integrations, and SQL Server optimization (stored procedures, triggers).",
                },
            ],
            Educations =
            [
                new Education
                {
                    InstitutionName = "[University Name], City, State",
                    Degree = "Master of Science in [Field]",
                    StartDate = new DateTime(2007, 1, 1),
                    EndDate = new DateTime(2010, 1, 1),
                    Description =
                        "<strong>State Ministry Recognition:</strong> "
                        + "Equivalent to State Bachelor's degree in Computer Science, State Ministry of Higher Education and Research)",
                },
            ],
            Projects =
            [
                new Project
                {
                    Name = "Project Name - Enterprise Resource Planning System",
                    Role = "Lead Developer",
                    StartDate = new DateTime(2024, 1, 1),
                    EndDate = new DateTime(2025, 1, 1),
                    Description =
                        "Full-stack ERP solution for retail/inventory management with <strong style=\"color:blue;font-weight:normal;\">sales, purchases, invoice generation, email campaigns, automated backups,</strong> customers, suppliers, and comprehensive reporting.\n"
                        + "Modular architecture with 5 projects (API, Server, Client, Components, Shared) following Clean Architecture principles.\n"
                        + "<strong style=\"color:blue;\">65+ secured REST APIs endpoints</strong> with role-based access control (Admin/Manager/User Roles).\n"
                        + "Multi-language support with resource localization and real-time SignalR notifications.\n"
                        + "Automated Google Drive backup integration with OAuth 2.0 authentication, scheduling, and Docker containerization.",
                    Technologies =
                        ".NET 9, Blazor Server, MudBlazor, Entity Framework Core, SQL Server, Docker, SignalR.",
                    Link = "Private Repository (Available upon request).",
                },
            ],
            Languages =
            [
                new Language { Name = "Language 1", Proficiency = "Native" },
                new Language { Name = "Language 2", Proficiency = "Fluent" },
                new Language { Name = "Language 3", Proficiency = "Conversational" },
                new Language { Name = "Language 4", Proficiency = "Intermediate - Module 3" },
            ],
            Interests =
            [
                new Interest { Name = "Software Architecture" },
                new Interest { Name = "AI & Machine Learning" },
                new Interest { Name = "Cybersecurity" },
                new Interest { Name = "Blockchain" },
                new Interest { Name = "Open Source Contribution" },
                new Interest { Name = "Chess" },
                new Interest { Name = "Hiking & Cycling" },
            ],
        };
}
