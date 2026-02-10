# Privacy & Data Handling — AiCV

## 1. User Data

* Stored locally in SQL database
* Includes profiles: personal details, experience, skills
* Editable and deletable at any time by the user

## 2. AI API Keys

* Provided by users
* Encrypted at rest
* Never exposed in logs, UI, or network traffic

## 3. AI Requests & Data Minimization

* Only structured profile data is sent to AI for generation
* Raw CVs or documents are never transmitted
* Users select provider/model; some models may use data for training
* AI responses are stored only if user saves the output

## 4. Job Data

* Scraped from URLs or manually input by user
* Used exclusively for generation
* Not transmitted to external services except the selected AI provider

## 5. Telemetry & External Services

* No automatic usage telemetry collected
* Optional Cloudflare Tunnel provides secure remote access only

## 6. User Control

* Users retain full control over:

  * AI providers and models used
  * Storage of generated CVs and cover letters
  * Hosting environment and deployment

## 7. Recommendations

* Use only trusted AI providers
* Review provider policies if using models that may retain or train on input data
* Cloudflare Tunnel is optional for remote access; custom networking is fully supported

## 8. Contact

For questions regarding privacy or data handling, please open a [GitHub Issue](https://github.com/FitimZulfiju/Web-CV-application-generator/issues).
