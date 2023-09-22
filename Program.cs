using Microsoft.Extensions.Configuration;
using MultithreadCallAPIs.Models;
using RestSharp;

class Program
{
    #region [AppSettings]

    private static IConfiguration? _configuration;
    public static string BaseUrlApi => _configuration["AppSettings:BaseUrlApi"];
    public static int MaxNumberConcurrentThreads => int.Parse(_configuration["AppSettings:MaxNumberConcurrentThreads"]);

    #endregion

    static async Task Main(string[] args)
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var posts = GetPosts();

        var semaphore = new SemaphoreSlim(MaxNumberConcurrentThreads);
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

        //TODO: Salvar dados no banco de dados

        Console.WriteLine("Todos posts e comentários carregados com sucesso!");
    }

    private static List<Post> GetPosts()
    {
        Console.WriteLine("Buscando lista de posts ...");
        Console.WriteLine("---");

        var client = new RestClient(BaseUrlApi);
        var request = new RestRequest("/posts", Method.Get);

        var response = client.Execute<List<Post>>(request);

        List<Post> posts = new();
        if (response.IsSuccessful)
        {
            if (response.Data != null)
            {
                posts = response.Data;
                Console.WriteLine($"{posts.Count} posts encontrados!");
                Console.WriteLine("---");
            }
            else
            {
                Console.WriteLine("Nenhum post encontrado na listagem de posts!");
                Console.WriteLine("---");
            }
        }
        else
        {
            Console.WriteLine($"Erro ao buscar lista de posts: {response.ErrorMessage}");
            Console.WriteLine("---");
        }

        return posts;
    }

    private static async Task<List<Comment>> GetCommentsAsync(Post post)
    {
        Console.WriteLine($"Buscando lista de comentários para o post id: {post.id} ...");
        Console.WriteLine("---");

        var client = new RestClient(BaseUrlApi);
        var request = new RestRequest($"comments?postId={post.id}", Method.Get);

        var response = await client.ExecuteAsync<List<Comment>>(request);

        List<Comment> comments = new();
        if (response.IsSuccessful)
        {
            if (response.Data != null)
            {
                comments = response.Data;
                Console.WriteLine($"{comments.Count} comentários encontrados para o post id: {post.id}!");
                Console.WriteLine("---");
            }
            else
            {
                Console.WriteLine($"Nenhum comentário encontrado para o post id: {post.id}");
                Console.WriteLine("---");
            }
        }
        else
        {
            Console.WriteLine($"Erro ao buscar lista de comentários para o post id: {post.id}. Erro: {response.ErrorMessage}");
            Console.WriteLine("---");
        }

        return comments;
    }
}