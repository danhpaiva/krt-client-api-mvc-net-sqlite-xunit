## 🚀 KRT.Cliente.API: Uma API de Clientes Construída com Qualidade e Performance

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/api.png?raw=true" width="700" alt="API">
</p>

Este repositório apresenta um projeto de API RESTful focada na gestão de dados de clientes (`Conta`), desenvolvido em **ASP.NET Core** e seguindo as práticas de engenharia de software para garantir escalabilidade, manutenibilidade e alta performance.

### 🌟 Pilares de Qualidade e Boas Práticas

O projeto KRT.Cliente.API é um exemplo de como a atenção às boas práticas transforma um sistema robusto e resiliente.

#### 1\. Código Limpo (Clean Code) e Padrões S.O.L.I.D.

Adotei a filosofia *Clean Code* para garantir que o código seja legível, conciso e autoexplicativo, reduzindo a dívida técnica a longo prazo.

  * **Responsabilidade Única (SRP):** Classes e métodos possuem responsabilidades bem definidas. Por exemplo, a camada de *Controller* foca apenas em roteamento e mapeamento de requisições, delegando a lógica de negócio e persistência.
  * **Nomeclatura Clara:** Variáveis, métodos e classes são nomeados de forma explícita, eliminando a necessidade de comentários excessivos.
  * **Tratamento de Erros:** Exceções são tratadas de forma controlada, com mensagens claras e *status codes* HTTP apropriados (`404 Not Found`, `400 Bad Request`, `204 No Content`).

#### 2\. Test-Driven Development (TDD) e Cobertura Total

A robustez da API é assegurada por uma bateria de testes unitários abrangentes, seguindo o ciclo **TDD**:

  * **Mocks e Isolamento:** Utilizamos o **Moq** para isolar o código de produção, garantindo que os testes de *Controller* e serviços se concentrem apenas na lógica a ser validada, sem dependência externa (como o serviço de cache ou o banco de dados real).
  * **Testes de Integração com EF Core InMemory:** Para validação da camada de persistência, utilizamos o provedor EF Core InMemory, simulando as interações com o banco de dados de forma rápida e controlada, crucial para verificar a concorrência e o ciclo de vida das entidades.
  * **Correção de Problemas de Rastreamento (EF Core):** O teste `PutConta` foi corrigido para explicitamente desanexar entidades do contexto (`EntityState.Detached`) antes de anexar uma nova versão, resolvendo problemas de rastreamento de concorrência comum em testes de `Update` com EF Core.

#### 3\. Performance e Escalabilidade com Cache Distribuído (Redis/IDistributedCache)

Para reduzir a latência e a pressão sobre o banco de dados(AWS/nuvem), implementei uma estratégia de *caching* avançada, seguindo o padrão **Cache-Aside**:

  * **Abstração com `IDistributedCache`:** O projeto utiliza a interface `IDistributedCache` do ASP.NET Core, permitindo a fácil substituição do provedor de cache (ex: de Redis para Memcached) sem alterar a lógica da aplicação.
  * **Estratégia de Cache Inteligente:** A API prioriza a leitura do cache e garante a invalidação consistente dos dados em operações de escrita (`POST`, `PUT`, `DELETE`), mantendo a precisão e a performance do sistema.

### 💾 Persistência de Dados: Adotando SQLite

O projeto utiliza o **SQLite** como motor de banco de dados para o desenvolvimento local e testes de integração, aproveitando suas características singulares:

  * **Zero Configuração (Serverless):** O SQLite dispensa um servidor de banco de dados dedicado. O banco de dados é armazenado em um único arquivo, o que elimina a complexidade de instalação, configuração e manutenção de instâncias de bancos de dados como SQL Server ou PostgreSQL.
  * **Portabilidade Imediata:** A facilidade de movimentação do arquivo do banco de dados torna o ambiente de desenvolvimento extremamente portátil e rápido de configurar em qualquer nova máquina.
  * **Agilidade em CI/CD e Testes:** Em pipelines de Integração Contínua/Entrega Contínua (CI/CD), a ausência de dependências de servidor para o banco de dados simplifica o ambiente de *build* e acelera a execução de testes de integração, contribuindo para ciclos de *feedback* mais rápidos.

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/database.png?raw=true" width="700" alt="Database">
</p>

-----

### 🛠️ Tecnologias Utilizadas

  * **Linguagem:** C\#
  * **Framework:** ASP.NET Core
  * **Persistência:** Entity Framework Core (EF Core) com **SQLite** e EF Core InMemory (para testes unitários)
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