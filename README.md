# Game Library API

API REST local para pesquisar jogos na IGDB e manter uma biblioteca pessoal em MongoDB. O projeto é voltado a uso pessoal e aprendizado; não inclui autenticação de usuários, hospedagem ou funcionalidades comerciais.

## Funcionalidades

- Pesquisa jogos na IGDB e consulta detalhes pelo ID externo.
- Importa automaticamente os metadados de um jogo para o MongoDB quando ele é consultado por ID ou adicionado à biblioteca.
- Adiciona jogos à biblioteca pessoal com status, avaliação e anotação.
- Lista, consulta, atualiza e remove jogos da biblioteca.
- Fornece estatísticas de quantidade e média das avaliações.
- Usa Twitch OAuth para autenticar as chamadas à IGDB.

## Tecnologias

- ASP.NET Core / .NET 10
- MongoDB
- IGDB API v4
- Twitch OAuth 2.0 (client credentials)
- AutoMapper para mapear entidades para DTOs

## Pré-requisitos

- .NET 10 SDK
- MongoDB local ou uma instância acessível pela máquina
- Credenciais de aplicação do Twitch Developer Portal com acesso à IGDB

## Configuração e execução

1. Na pasta `GameLibraryApi`, crie ou edite `appsettings.Secrets.json`:

   ```json
   {
     "MongoDb": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "gamelibraryapi"
     },
     "Igdb": {
       "ClientId": "<seu-client-id>",
       "ClientSecret": "<seu-client-secret>"
     }
   }
   ```

   O arquivo está no `.gitignore`, é carregado apenas em ambiente `Development` e não é copiado para as saídas de build ou publicação. Não adicione credenciais ao `appsettings.json` nem as compartilhe. Em outros ambientes, forneça `MongoDb__ConnectionString`, `MongoDb__DatabaseName`, `Igdb__ClientId` e `Igdb__ClientSecret` por variáveis de ambiente ou por um provedor seguro de configuração.

2. Inicie o MongoDB configurado.
3. Restaure, compile e execute a API:

   ```powershell
   dotnet restore
   dotnet build
   dotnet run --urls http://localhost:5080
   ```

   Execute os comandos dentro da pasta `GameLibraryApi`. A API estará disponível em `http://localhost:5080`.

Em `Development`, o documento OpenAPI fica disponível em `/openapi/v1.json`. Há exemplos de requisições no arquivo `GameLibraryApi.http`.

## Endpoints

| Método | Rota | Descrição |
| --- | --- | --- |
| `GET` | `/` | Verifica se a API está ativa |
| `GET` | `/api/games/search?query=...&limit=20` | Pesquisa jogos na IGDB; `limit` aceita de 1 a 50 |
| `GET` | `/api/games/{externalId}` | Obtém detalhes de um jogo e importa seus metadados localmente se necessário |
| `GET` | `/api/library` | Lista os jogos da biblioteca |
| `GET` | `/api/library/{externalGameId}` | Obtém um jogo da biblioteca |
| `POST` | `/api/library` | Importa e adiciona um jogo à biblioteca |
| `PATCH` | `/api/library/{externalGameId}` | Atualiza status, avaliação ou anotação |
| `DELETE` | `/api/library/{externalGameId}` | Remove um jogo da biblioteca |
| `GET` | `/api/library/statistics` | Retorna total, média de avaliações e quantidade por status |

### Exemplos

Pesquisar um jogo:

```http
GET http://localhost:5080/api/games/search?query=Elden%20Ring&limit=5
```

Adicionar à biblioteca usando o ID externo retornado pela pesquisa:

```http
POST http://localhost:5080/api/library
Content-Type: application/json

{
  "externalGameId": 119133,
  "status": "playing",
  "personalRating": 9,
  "note": "Minha primeira campanha"
}
```

Os status aceitos são `wantToPlay`, `playing`, `onHold`, `finished` e `dropped`. A avaliação é opcional e aceita valores de 0 a 10; a anotação é opcional e aceita até 2000 caracteres.

Atualizar parcialmente uma entrada:

```http
PATCH http://localhost:5080/api/library/119133
Content-Type: application/json

{
  "status": "finished",
  "personalRating": 10
}
```

## Persistência e comportamento

- A biblioteca é única e pessoal; não há autenticação nem separação por usuário.
- A pesquisa consulta a IGDB sob demanda e não importa todo o catálogo.
- Os metadados importados são guardados na coleção `games`; as entradas pessoais ficam na coleção `userGames`.
- A remoção exclui somente a entrada da biblioteca. Os metadados do jogo permanecem no MongoDB para reutilização.
- Índices únicos são criados para o ID externo do jogo e para impedir que o mesmo jogo seja adicionado mais de uma vez à biblioteca.
- Os DTOs são mapeados com perfis AutoMapper registrados na injeção de dependência.

## Observações

- Os endpoints que dependem da IGDB precisam de credenciais Twitch válidas. Problemas de configuração do provedor retornam `503`; falhas de comunicação ou resposta do provedor retornam `502`.
- O projeto não inclui um projeto de testes automatizados no momento.
