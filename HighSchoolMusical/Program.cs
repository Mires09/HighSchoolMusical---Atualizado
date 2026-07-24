using HighSchoolMusical.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.WebHost.UseUrls("http://0.0.0.0:5050");

var app = builder.Build();

app.UseCors("AllowAll");

Musica[] listamusicas = new Musica[100];

int totalMusicas = 0;

Playlist[] listasPlaylists = new Playlist[100];

int totalPlaylists = 0;

// Normalizar texto para buscas ignorando acentos e letras maiúsculas
string NormalizarTexto(string texto)
{
    return texto
        .Trim()
        .ToLower()
        .Normalize(System.Text.NormalizationForm.FormD)
        .Replace("\u0300", "")
        .Replace("\u0301", "")
        .Replace("\u0302", "")
        .Replace("\u0303", "")
        .Replace("\u0308", "");
}

app.MapGet("/", () =>
{
    return Results.Ok("Sua música está harmônica!");
});

//Cadastrar novas músicas
app.MapPost("/cadastrarmusica", (JsonElement body) =>
{
    Musica musica = new Musica();

    musica.Id = totalMusicas + 1;
    musica.Titulo = body.GetProperty("titulo").GetString();
    musica.Ano = body.GetProperty("ano").GetInt16();
    musica.Compositor = body.GetProperty("compositor").GetString();
    musica.Genero = body.GetProperty("genero").GetString();
    musica.Artista = body.GetProperty("artista").GetString();

    listamusicas[totalMusicas] = musica;
    totalMusicas++;

    return Results.Ok(new
{
    musica
    });
});


//Listar as músicas cadastradas
app.MapGet("/listarmusicas", () =>
{
    if (totalMusicas == 0)
    {
        return Results.Ok(new
        {
            mensagem = "Não há nenhuma música cadastrada no momento!",
            musicas = Array.Empty<Musica>()
        });
    }
    
    Musica[] musicaCadastrados = new Musica[totalMusicas];

    for (int i = 0; i < totalMusicas; i++)
{
    musicaCadastrados[i] = listamusicas[i];
}

return Results.Ok(new
{
    musicas = musicaCadastrados
    });
});

//Buscar Músicas
app.MapGet("/musica/busca", (string titulo) =>
{
    Musica[] musicasEncontradas = new Musica[totalMusicas];

    int totalEncontradas = 0;

    for (int i = 0; i < totalMusicas; i++)
    {
        if (listamusicas[i].Titulo?.ToLower() == titulo.ToLower())
        {
            musicasEncontradas[totalEncontradas] = listamusicas[i];
            totalEncontradas++;
        }
    }

    if (totalEncontradas > 0)
    {
        Musica[] resultadoFinal = new Musica[totalEncontradas];

        for (int i = 0; i < totalEncontradas; i++)
        {
            resultadoFinal[i] = musicasEncontradas[i];
        }        

        return Results.Ok(new
        {
            titulo,
            musicas = resultadoFinal
        });
    } 

    return Results.NotFound(new
    {
        mensagem = "Nenhuma música com esse nome foi encontrada!"
    });
});

// Buscar música pelo ID
app.MapGet("/buscarid/{id}", (int id) =>
{
    for (int i = 0; i < totalMusicas; i++)
    {
        if (listamusicas[i].Id == id)
        {
            return Results.Ok(listamusicas[i]);
        }
    }

    return Results.NotFound(new
    {
        mensagem = "Música não encontrada!"
    });
});

//Modificar apenas título da música
app.MapPatch("/modificartitulo/{id}", (int id, JsonElement body) =>
{
    Musica? musica = null;


    for (int i = 0; i < totalMusicas; i++)
    {
        if (listamusicas[i].Id == id)
        {
            musica = listamusicas[i];


            if (body.TryGetProperty("titulo", out var titulo))
            {
                musica.Titulo = titulo.GetString();
            }


            listamusicas[i] = musica;

            break;
        }
    }


    if (musica == null)
    {
        return Results.NotFound(new
        {
            mensagem = "Música não encontrada!"
        });
    }


    return Results.Ok(new
    {
        mensagem = "Título atualizado com sucesso!",
        musica
    });

});

//Deletar músicas
app.MapDelete("/deletarmusica/{id}", (int id) =>
{
    int index = -1;

    // Procurar músicas pelo Id
    for (int i = 0; i < totalMusicas; i++)
    {
        if (listamusicas[i].Id == id)
        {
            index = i;
            break;
        }
    }

    if (index == -1)
    {
        return Results.NotFound(new { 
            mensagem = "Música não encontrada!" 
    });
    }

    // Remove a música de todas as playlists

for (int i = 0; i < totalPlaylists; i++)
{
    listasPlaylists[i].Musicas.RemoveAll(m => m.Id == id);
}

// Remove da lista principal

for (int i = index; i < totalMusicas - 1; i++)
{
    listamusicas[i] = listamusicas[i + 1];
}

listamusicas[totalMusicas - 1] = null!;

totalMusicas--;

return Results.Ok(new
{
    mensagem = "Música removida com sucesso!"
});
});

// Criar Playlists
app.MapPost("/criarplaylist", (Playlist playlist) =>
{
    if (totalPlaylists >= listasPlaylists.Length)
    {
        return Results.BadRequest("Limite de playlists atingido!");
    }

    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Nome.Trim().ToLower() ==
            playlist.Nome.Trim().ToLower())
        {
            return Results.BadRequest("Já existe uma playlist com esse nome!");
        }
    }

    playlist.Id = totalPlaylists + 1;

    playlist.Musicas ??= new List<Musica>();

    listasPlaylists[totalPlaylists] = playlist;

    totalPlaylists++;

    return Results.Ok(playlist);
});

// Listar Playlists
app.MapGet("/listarplaylists", () =>
{
    Playlist[] resultado = new Playlist[totalPlaylists];

    for (int i = 0; i < totalPlaylists; i++)
    {
        resultado[i] = listasPlaylists[i];
    }

    return Results.Ok(new
    {
        quantidade = totalPlaylists,
        mensagem = totalPlaylists == 0
            ? "Não há nenhuma playlist criada no momento!"
            : "Playlists carregadas com sucesso.",

        playlists = resultado
    });
});

// Adicionar música à playlist
app.MapPost("/playlist/{playlistId}/adicionar/{musicaId}",
(int playlistId, int musicaId) =>
{
    Playlist? playlist = null;

    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == playlistId)
        {
            playlist = listasPlaylists[i];
            break;
        }
    }

    if (playlist == null)
    {
        return Results.NotFound("Playlist não encontrada!");
    }

    Musica? musica = null;

    for (int i = 0; i < totalMusicas; i++)
    {
        if (listamusicas[i].Id == musicaId)
        {
            musica = listamusicas[i];
            break;
        }
    }

    if (musica == null)
    {
        return Results.NotFound("Música não encontrada!");
    }

    foreach (var item in playlist.Musicas)
    {
        if (item.Id == musica.Id)
        {
            return Results.BadRequest("Essa música já está na playlist!");
        }
    }

    playlist.Musicas.Add(musica);

    return Results.Ok(playlist);
});

// Buscar playlist por ID
app.MapGet("/playlist/{id}", (int id) =>
{
    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == id)
        {
            return Results.Ok(listasPlaylists[i]);
        }
    }

    return Results.NotFound("Playlist não encontrada!");
});

// Listar músicas de uma playlist

app.MapGet("/playlist/{id}/musicas", (int id) =>
{
    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == id)
        {
            return Results.Ok(new
            {
                musicas = listasPlaylists[i].Musicas
            });
        }
    }

    return Results.NotFound(new
    {
        mensagem = "Playlist não encontrada!"
    });
});

// Buscar playlist por nome ignorando maiúsculas, minúsculas e acentos

app.MapGet("/playlist/nome/{nome}", (string nome) =>
{
    string pesquisa = NormalizarTexto(nome);


    for (int i = 0; i < totalPlaylists; i++)
    {
        string nomePlaylist = NormalizarTexto(listasPlaylists[i].Nome);


        if (nomePlaylist == pesquisa)
        {
            return Results.Ok(listasPlaylists[i]);
        }
    }


    return Results.NotFound(new
    {
        mensagem = "Playlist não encontrada!"
    });

});

// Pesquisar playlists sem precisar escrever o nome inteiro
// Ignora maiúsculas e acentos

app.MapGet("/playlists/pesquisar/{texto}", (string texto) =>
{
    string pesquisa = NormalizarTexto(texto);


    List<Playlist> resultado = new();


    for (int i = 0; i < totalPlaylists; i++)
    {
        string nomePlaylist = NormalizarTexto(listasPlaylists[i].Nome);


        if (nomePlaylist.Contains(pesquisa))
        {
            resultado.Add(listasPlaylists[i]);
        }
    }


    return Results.Ok(new
    {
        quantidade = resultado.Count,
        playlists = resultado
    });

});

// Renomear playlist
app.MapPut("/playlist/{id}", (int id, Playlist dados) =>
{
    Playlist? playlist = null;

    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == id)
        {
            playlist = listasPlaylists[i];
            break;
        }
    }

    if (playlist == null)
    {
        return Results.NotFound("Playlist não encontrada!");
    }

    playlist.Nome = dados.Nome;

    return Results.Ok(playlist);
});

// Remover música da playlist
app.MapDelete("/playlist/{playlistId}/remover/{musicaId}",
(int playlistId, int musicaId) =>
{
    Playlist? playlist = null;

    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == playlistId)
        {
            playlist = listasPlaylists[i];
            break;
        }
    }

    if (playlist == null)
        return Results.NotFound("Playlist não encontrada!");

    Musica? musica = null;

    foreach (var item in playlist.Musicas)
    {
        if (item.Id == musicaId)
        {
            musica = item;
            break;
        }
    }

    if (musica == null)
        return Results.NotFound("Música não encontrada na playlist!");

    playlist.Musicas.Remove(musica);

    return Results.Ok(playlist);
});

// Excluir playlist
app.MapDelete("/playlist/{id}", (int id) =>
{
    int indice = -1;

    for (int i = 0; i < totalPlaylists; i++)
    {
        if (listasPlaylists[i].Id == id)
        {
            indice = i;
            break;
        }
    }

    if (indice == -1)
    {
        return Results.NotFound("Playlist não encontrada!");
    }

    for (int i = indice; i < totalPlaylists - 1; i++)
    {
        listasPlaylists[i] = listasPlaylists[i + 1];
    }

        listasPlaylists[totalPlaylists - 1] = new Playlist();

    totalPlaylists--;

    return Results.Ok("Playlist removida com sucesso!");
});

app.Run();