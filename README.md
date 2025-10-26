# 🚀 KRT.Cliente.API: <br> Uma API de Clientes Construída com .NET e Redis

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/api.png?raw=true" width="700" alt="API RESTful">
</p>

Este repositório apresenta um projeto de API RESTful focada na gestão de dados de clientes (`Conta`), desenvolvido em **ASP.NET Core** e seguindo as melhores práticas de engenharia de software para garantir escalabilidade, manutenibilidade e alta performance.

## ✨ Pilares de Qualidade e Boas Práticas

O projeto KRT.Cliente.API demonstra uma arquitetura robusta e resiliente através da aplicação rigorosa de boas práticas:

### 1. 🧼 Código Limpo (Clean Code) e Padrões S.O.L.I.D.

Adotei a filosofia *Clean Code* para garantir que o código seja legível, conciso e autoexplicativo, minimizando a dívida técnica a longo prazo.

  * **Responsabilidade Única (SRP):** Classes e métodos possuem responsabilidades bem definidas. A camada de *Controller*, por exemplo, foca em roteamento e mapeamento, delegando a lógica de negócio e persistência.
  * **Nomeclatura Clara:** Variáveis, métodos e classes são nomeados de forma explícita.
  * **Tratamento de Erros:** Exceções são tratadas de forma controlada, retornando *status codes* HTTP apropriados (`404 Not Found`, `400 Bad Request`, `204 No Content`).

### 2. ✅ Test-Driven Development (TDD) e Cobertura Total

A robustez da API é assegurada por uma bateria de testes unitários e de integração, seguindo o ciclo **TDD**:

  * **Mocks e Isolamento (Moq):** Utilização do Moq para isolar o código de produção, garantindo que os testes de *Controller* e serviços se concentrem apenas na lógica de negócio, sem dependência externa (cache ou banco de dados real).
  * **Testes de Integração (EF Core InMemory):** Validação da camada de persistência simulando interações com o banco de dados de forma rápida e controlada, crucial para verificar a concorrência e o ciclo de vida das entidades.
  * **Correção de Problemas de Rastreamento (EF Core):** O fluxo de `Update` foi ajustado para garantir o desanexamento explícito de entidades do contexto, resolvendo problemas de rastreamento de concorrência.

### 3. 🚀 Performance e Escalabilidade com Cache Distribuído (Redis)

Para reduzir a latência e a pressão sobre o banco de dados principal, foi implementada uma estratégia de *caching* avançada, seguindo o padrão **Cache-Aside**:

  * **Abstração com `IDistributedCache`:** O projeto utiliza a interface `IDistributedCache` do ASP.NET Core, permitindo a fácil substituição do provedor de cache (ex: de Redis para Memcached) sem alterar a lógica de domínio.
  * **Estratégia de Cache Inteligente:** A API prioriza a leitura do cache e garante a invalidação consistente dos dados em operações de escrita, mantendo a precisão e a performance do sistema.

***

## 🎯 Detalhes da Implementação de Cache na `ContasController`

A `ContasController` utiliza o Redis para otimizar *endpoints* de leitura custosos, garantindo alta performance através de uma gestão rigorosa de consistência e latência.

### A. Métodos de Leitura (Estratégia Cache-Aside)

Estes *endpoints* buscam a informação no Redis. Se não existir (*cache miss*), a consulta é feita no banco, e o resultado é salvo no cache com um TTL (Tempo de Vida) antes de ser retornado.

| Método (HTTP GET)       | Chave do Cache no Redis | Propósito da Otimização                                              |
| :---------------------- | :---------------------- | :------------------------------------------------------------------- |
| `GetContasAtivas()`     | `"ContasAtivas"`        | Otimiza a busca de lista de todas as contas ativas.                  |
| `GetContasInativas()`   | `"ContasInativas"`      | Otimiza a busca de lista de todas as contas inativas.                |
| `GetResumoStatus()`     | `"ResumoStatus"`        | Otimiza o cálculo de totais de contas ativas e inativas (agregação). |
| `GetContaPorCPF(cpf)`   | `conta:{cpf}`           | Otimiza a busca individual de uma conta por CPF.                     |
| `GetTotaisPorAno(anos)` | `TotaisPorAno:A,B...`   | Otimiza consultas de agregação complexa (`GroupBy`, `Count`).        |

### B. Invalidação de Cache (Consistência)

A consistência dos dados é garantida pela função auxiliar `InvalidateCaches()`, que é chamada após qualquer modificação no banco de dados. Esta invalidação remove chaves específicas (`"ResumoStatus"`, `"ContasAtivas"`, `"ContasInativas"` e `conta:{cpf}`) para forçar a atualização do cache na próxima leitura.

| Métodos de Escrita que Invalidam o Cache     | Ação no Banco de Dados                   |
| :------------------------------------------- | :--------------------------------------- |
| `PostConta()`, `PutConta()`, `DeleteConta()` | Criação, Atualização e Deleção de conta. |
| `AtivarConta()`, `InativarConta()`           | Modificação do status de uma conta.      |
| `SoftDelete()`, `RestaurarConta()`           | Modificação do status de deleção lógica. |

***

### 💾 Persistência de Dados: Adotando SQLite

O projeto utiliza o **SQLite** como motor de banco de dados para o desenvolvimento local e testes de integração, aproveitando suas características singulares:

  * **Zero Configuração (Serverless):** O SQLite dispensa um servidor de banco de dados dedicado. O banco de dados é armazenado em um único arquivo, eliminando a complexidade de instalação e manutenção.
  * **Portabilidade Imediata:** A facilidade de movimentação do arquivo do banco de dados torna o ambiente de desenvolvimento extremamente portátil e rápido de configurar.
  * **Agilidade em CI/CD e Testes:** A ausência de dependências de servidor para o banco de dados acelera a execução de testes de integração em *pipelines* de CI/CD.

<p align="center">
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/database.png?raw=true" width="700" alt="Arquitetura do Database">
</p>

-----

### 🛠️ Tecnologias Utilizadas

  * **Linguagem:** C\#
  * **Framework:** ASP.NET Core
  * **Persistência:** Entity Framework Core (EF Core) com **SQLite** e EF Core InMemory (para testes)
  * **Testes:** xUnit, Moq
  * **Cache:** `IDistributedCache` (Pronto para Redis)

### 🚀 Como Executar o Projeto

1.  **Clone o repositório:**

    ```bash
    git clone [URL_DO_SEU_REPOSITORIO]
    cd KRT.Cliente.API
    ```

2.  **Restaure as dependências:**

    ```bash
    dotnet restore
    ```

3.  **Execute a API:**

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
   <img src="https://github.com/danhpaiva/krt-client-api-mvc-net-sqlite-xunit/blob/main/src/testes_unitarios.png?raw=true" width="700" alt="Clique aqui para ver a imagem dos Testes">
</p>

## 👤 Desenvolvedor

Este projeto foi desenvolvido por:

  * **Nome:** Daniel Paiva
  * **LinkedIn:** [https://www.linkedin.com/in/danhpaiva/](https://www.linkedin.com/in/danhpaiva/)

Sinta-se à vontade para conectar-se e discutir padrões de arquitetura e resiliência\!

***Criado com ❤️ e .NET***