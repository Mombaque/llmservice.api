# LlmService API

`LlmService` is the trust boundary for LLM provider access. Consuming APIs authenticate to this gateway and send LLM requests, but they do not manage or choose provider API keys directly.

## Environment Variables

Required for `LlmService`:

- `Jwt__Key`: shared HMAC signing key, minimum 32 characters
- `Jwt__Issuer`: expected token issuer
- `Jwt__Audience`: expected token audience
- `OpenAI__ApiKey`: OpenAI API key used only by `LlmService`
- `OpenAI__DefaultModel`: default OpenAI model
- `OpenAI__BaseUrl`: defaults to `https://api.openai.com/v1/`
- `OpenAI__TimeoutSeconds`: request timeout in seconds

Security notes:

- Do not commit production JWT secrets.
- Generate production `Jwt__Key` securely.
- Do not expose `OpenAI__ApiKey` to consuming APIs.
- Do not log JWTs or provider API keys.

## Authentication

All LLM gateway endpoints require a valid Bearer token.

Required JWT claims:

- `iss`: must match `Jwt__Issuer`
- `aud`: must match `Jwt__Audience`
- `service`: must be one of:
  - `morita-api`
  - `promotora-api`
  - `clinic-api`

`LlmService` validates tokens only. Consuming APIs are responsible for generating and sending the token.

## Public And Private Endpoints

- Public: `GET /health`
- Private: `POST /v1/chat/completions`

## Example Request

```bash
curl -X POST "http://localhost:5004/v1/chat/completions" \
  -H "Authorization: Bearer <jwt-token>" \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {
        "role": "user",
        "content": [
          {
            "type": "text",
            "text": "Hello"
          }
        ]
      }
    ]
  }'
```

## Consuming API Guidance

Each consuming API should:

1. Generate a JWT signed with the shared `Jwt__Key`.
2. Set `iss` to `Jwt__Issuer`.
3. Set `aud` to `Jwt__Audience`.
4. Set the `service` claim to its service name.
5. Send `Authorization: Bearer <token>` on requests to `LlmService`.
6. Cache the token until shortly before expiration instead of generating one per request.
