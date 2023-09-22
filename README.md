# Multithread Call APIs
Consultas concorrentes em APIs utilizando Multithread. Percorre uma lista de posts retornados da API, buscando os comentários de cada post, com limitação de chamadas concorrentes configurável no appsettings.

# Base API
* https://jsonplaceholder.typicode.com/

# Bibliotecas
* RestSharp
* Newtonsoft.Json
* SemaphoreSlim

# Trecho de código
```c#
var posts = GetPosts(); // Lista de posts da API

var semaphore = new SemaphoreSlim(MaxNumberConcurrentThreads); // Seta a quantidade máxima de Task concorrentes
var tasks = new List<Task>();

foreach (var post in posts)
{
    await semaphore.WaitAsync(); // Aguarde até que um slot esteja disponível no semaphore

    tasks.Add(Task.Run(async () =>
    {
        try
        {
            var comments = await GetCommentsAsync(post);
            post.Comments = comments;
        }
        finally
        {
            semaphore.Release(); // Libere o slot para a próxima tarefa
        }
    }));
}

await Task.WhenAll(tasks); // Aguarde todas as tarefas serem concluídas
```
