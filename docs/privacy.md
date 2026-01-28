# Privacy & Data Handling

## 1. User Data

- Stored locally in SQL database
- Includes profiles: personal details, experience, skills
- Editable and deletable at any time

## 2. AI API Keys

- Provided by users
- Encrypted at rest
- Not exposed in logs, UI, or network

## 3. AI Requests & Data Minimization

- Only structured profile info sent to AI
- No raw CVs or documents sent
- Users choose provider/model; some may use data for training
- AI responses stored only if user saves output

## 4. Job Data

- Scraped from URLs or manually input
- Used solely for generation
- Not sent to external services besides AI provider

## 5. Telemetry & External Services

- No usage telemetry
- Optional Cloudflare Tunnel only provides secure remote access

## 6. User Control

- Full control over:
  - AI providers/models used
  - Generated CV/cover letter storage
  - Hosting environment

## 7. Recommendations

- Use trusted AI providers
- Understand provider policies if using models that may train on input
- Optional Cloudflare Tunnel for remote access or configure your own networking

## 8. Contact

For any privacy or data-handling inquiries, please open a [GitHub Issue](https://github.com/FitimZulfiju/Web-CV-application-generator/issues).
