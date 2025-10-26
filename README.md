## 🚀 KRT.Cliente.API: <br>Uma API de Clientes Construída com Qualidade e Performance

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/api.png?raw=true" width="700" alt="API">
</p>

Este repositório apresenta um projeto de API RESTful focada na gestão de dados de clientes (`Conta`), desenvolvido em **ASP.NET Core** e seguindo as mais rigorosas práticas de engenharia de software para garantir escalabilidade, manutenibilidade e alta performance.

### 🌟 Pilares de Qualidade e Boas Práticas

O projeto KRT.Cliente.API não é apenas um código funcional; é um exemplo de como a atenção às boas práticas transforma um sistema robusto e resiliente.

#### 1\. Código Limpo (Clean Code) e Padrões S.O.L.I.D.

Adotamos a filosofia *Clean Code* para garantir que o código seja legível, conciso e autoexplicativo, reduzindo a dívida técnica a longo prazo.

  * **Responsabilidade Única (SRP):** Classes e métodos possuem responsabilidades bem definidas. Por exemplo, a camada de *Controller* foca apenas em roteamento e mapeamento de requisições, delegando a lógica de negócio e persistência.
  * **Nomeclatura Clara:** Variáveis, métodos e classes são nomeados de forma explícita, eliminando a necessidade de comentários excessivos.
  * **Tratamento de Erros:** Exceções são tratadas de forma controlada, com mensagens claras e *status codes* HTTP apropriados (`404 Not Found`, `400 Bad Request`, `204 No Content`).

#### 2\. Test-Driven Development (TDD) e Cobertura Total

A robustez da API é assegurada por uma bateria de testes unitários abrangentes, seguindo o ciclo **TDD**:

  * **Mocks e Isolamento:** Utilizamos o **Moq** para isolar o código de produção, garantindo que os testes de *Controller* e serviços se concentrem apenas na lógica a ser validada, sem dependência externa (como o serviço de cache ou o banco de dados real).
  * **Testes de Integração com EF Core InMemory:** Para validação da camada de persistência, utilizamos o provedor EF Core InMemory, simulando as interações com o banco de dados de forma rápida e controlada, crucial para verificar a concorrência e o ciclo de vida das entidades.
  * **Correção de Problemas de Rastreamento (EF Core):** O teste `PutConta` foi corrigido para explicitamente desanexar entidades do contexto (`EntityState.Detached`) antes de anexar uma nova versão, resolvendo problemas de rastreamento de concorrência comum em testes de `Update` com EF Core.

#### 3\. Performance e Escalabilidade com Cache Distribuído (Redis/IDistributedCache)

Para reduzir a latência e a pressão sobre o banco de dados, implementamos uma estratégia de *caching* avançada, seguindo o padrão **Cache-Aside**:

  * **Abstração com `IDistributedCache`:** O projeto utiliza a interface `IDistributedCache` do ASP.NET Core, permitindo a fácil substituição do provedor de cache (ex: de Redis para Memcached) sem alterar a lógica da aplicação.
  * **Estratégia de Cache Inteligente:**
      * **Leitura (Hit/Miss):** A API primeiro verifica o cache (`GetAsync`). Se o dado for encontrado (*Hit*), ele é retornado instantaneamente. Se não (*Miss*), o dado é buscado no banco de dados e salvo no cache (`SetAsync`) para consultas futuras.
      * **Invalidação de Cache:** Em operações de escrita (`POST`, `PUT`, `DELETE`), todos os caches relevantes (p. ex., cache da conta específica, lista de contas ativas, resumo de status) são **invalidados** (`RemoveAsync`), garantindo a consistência dos dados nas próximas leituras.
  * **Mocking de Extensões de Cache:** Os testes foram adaptados para interagir diretamente com os métodos base assíncronos (`GetAsync`/`SetAsync`) da interface `IDistributedCache`, contornando a limitação do Moq em relação a métodos de extensão (`GetStringAsync`/`SetStringAsync`), o que é uma prática avançada de *mocking* em .NET.

-----

### 🛠️ Tecnologias Utilizadas

  * **Linguagem:** C\#
  * **Framework:** ASP.NET Core
  * **Persistência:** Entity Framework Core (EF Core)
  * **Testes:** xUnit, Moq
  * **Cache:** `IDistributedCache` (Pronto para Redis)

### 🚀 Como Executar o Projeto

1.  **Clone o repositório:**

    ```bash
    git clone [URL_DO_SEU_REPOSITORIO]
    cd KRT.Cliente.API
    ```

2.  **Restaure as dependências:**

    ```bash
    dotnet restore
    ```

3.  **Execute a API:**

    ```bash
    dotnet run --project KRT.Cliente.Api
    ```

    A API estará acessível em `https://localhost:PORTA_DA_API/api/contas`.

### ✅ Executando os Testes

Para validar a integridade e a qualidade do código, execute o conjunto completo de testes a partir da linha de comando:

```bash
dotnet test KRT.Cliente.API.Test
```

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/testes_unitarios.png?raw=true" width="700" alt="Testes">
</p>