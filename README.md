# Modernizacao de Sistema Legado - Cooperativa Genesis

Projeto Final do treinamento **Acelera Maker / Montreal - COBOL**, com foco na modernizacao de um sistema legado de cadastro de clientes, integrando uma API ASP.NET Core 8 a um componente COBOL existente (GnuCOBOL), preservando o processamento legado e adicionando uma camada moderna de acesso.

## Cenario

A Cooperativa Genesis possui um sistema legado responsavel pelo cadastro de clientes. O objetivo deste projeto e permitir que atendentes consultem e atualizem dados de clientes atraves de uma API moderna, mantendo compatibilidade com o processamento COBOL existente, sem substitui-lo.

## Arquitetura

```
Swagger UI --> ASP.NET Core 8 (ClientesApi) --> GnuCOBOL (CLIENTES.exe) --> MySQL (BankSystem.clientes)
```

- A API recebe as requisicoes HTTP (GET e PUT).
- O componente `CobolService` monta uma mensagem no protocolo pipe-delimited e a envia ao executavel `CLIENTES.exe` via STDIN, lendo a resposta via STDOUT.
- Os dados sao persistidos/consultados no MySQL, banco `BankSystem`, tabela `clientes`.

Detalhes completos da arquitetura, decisoes tecnicas e protocolo de comunicacao estao documentados em [`docs/Documento_Arquitetura.pdf`](docs/Documento_Arquitetura.pdf) e [`docs/Estrutura_Compartilhada.pdf`](docs/Estrutura_Compartilhada.pdf).

## Stack Tecnologica

| Camada | Tecnologia |
|---|---|
| API | ASP.NET Core 8 Web API |
| Processamento legado | GnuCOBOL 2.0 (32 bits) |
| Banco de dados | MySQL |
| Testes automatizados | xUnit + Moq |
| Documentacao da API | Swagger / OpenAPI |

## Estrutura do Projeto

```
legacy-client-modernization/
├── api/ClientesApi/
│   ├── Controllers/ClientesController.cs
│   ├── Models/Cliente.cs
│   ├── Services/
│   │   ├── ClienteRepository.cs
│   │   └── CobolService.cs
│   ├── Program.cs
│   └── appsettings.json
├── cobol/
│   ├── CLIENTES.cbl
│   └── bin/CLIENTES.exe
├── testes/ClientesApi.Testes/
│   └── ClientesControllerTests.cs
└── docs/
    ├── Documento_Arquitetura.pdf
    ├── Estrutura_Compartilhada.pdf
    ├── Plano_de_Testes.pdf
    └── Relatorio_Uso_IA.pdf
```

## Endpoints

| Metodo | Rota | Descricao |
|---|---|---|
| GET | `/api/Clientes/{codigo}` | Consulta os dados cadastrais de um cliente pelo codigo |
| PUT | `/api/Clientes/{codigo}` | Atualiza telefone e e-mail de um cliente existente |

## Como Executar

### Pre-requisitos

- .NET 8 SDK
- GnuCOBOL 2.0 (32 bits) instalado, com o diretorio `bin` do GnuCOBOL adicionado ao `PATH` do sistema
- MySQL Server instalado e em execucao, com o banco `BankSystem` e a tabela `clientes` criados
- Connection string `MySql` configurada em `api/ClientesApi/appsettings.json` (a API acessa o MySQL diretamente via `MySql.Data.MySqlClient`, sem necessidade de driver ODBC)
- Caminho do executável `CLIENTES.exe` configurado em `api/ClientesApi/appsettings.json` na chave `Cobol:ExePath`

### Passos

```bash
# 1. Compilar o programa COBOL (se necessario)
cd cobol
cobc -x -o bin/CLIENTES.exe CLIENTES.cbl

# 2. Conferir o caminho do executavel em appsettings.json
# Chave "Cobol:ExePath" deve apontar para o caminho absoluto do CLIENTES.exe
# gerado no passo anterior. Esse caminho e especifico de cada maquina.

# 3. Restaurar e rodar a API
cd ../api/ClientesApi
dotnet restore
dotnet run
```

A API sobe por padrao em `http://localhost:5077`. O Swagger esta configurado na raiz do projeto (`RoutePrefix` vazio em `Program.cs`), entao a interface fica disponivel diretamente em `http://localhost:5077`.

### Rodando os testes automatizados

```bash
cd testes/ClientesApi.Testes
dotnet test
```

Todos os 8 testes (3 unitarios + 5 de integracao mockada) devem passar. Uma entrada de log referente ao cenario de falha simulada do COBOL e esperada e nao indica erro na suite.

## Documentacao

Todos os entregaveis exigidos pelo projeto estao na pasta [`docs/`](docs/):

- **Documento de Arquitetura** - arquitetura escolhida, justificativas tecnicas, fluxo de execucao e componentes
- **Estrutura Compartilhada** - definicao do protocolo de dados usado na integracao .NET <-> COBOL
- **Plano de Testes** - casos de teste, criterios de aceitacao e evidencias de execucao
- **Relatorio de Uso de IA** - prompts utilizados durante o desenvolvimento, com analise critica de cada resposta

## Autor

Davi - Acelera Maker / Montreal, Projeto Final COBOL