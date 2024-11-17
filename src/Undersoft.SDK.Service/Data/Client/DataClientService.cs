using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Undersoft.SDK.Service.Data.Client
{
    public partial class DataClientService : HttpClient
    {
        public DataClientService(Uri serviceUri)
        {
            if (serviceUri == null)
                throw new ArgumentNullException(nameof(serviceUri));

            CommandRegistry = new Registry<Arguments>(true);

            BaseAddress = new Uri(serviceUri.OriginalString + "/");
            DefaultRequestVersion = HttpVersion.Version20;
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            Timeout = TimeSpan.FromMinutes(5);
            this.DefaultRequestHeaders.Add("page", "0");
            this.DefaultRequestHeaders.Add("limit", "0");
            this.DefaultRequestHeaders.Remove(@"Accept");
            this.DefaultRequestHeaders.Add(@"Accept", @"application/json");
        }

        public void SetAuthorization(string token)
        {
            if (token != null)
            {
                this.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public void SetPagination(int page, int limit)
        {
            if (page > 0 && limit > 0)
            {
                Page = page;
                Limit = limit;
            }
        }

        public int Page
        {
            get => Int32.Parse(this.DefaultRequestHeaders.Where(h => h.Key == "page").First().Value.First());
            set
            {
                this.DefaultRequestHeaders.Remove("page"); this.DefaultRequestHeaders.Add("page", value.ToString());
            }
        }
        public int Limit
        {
            get => Int32.Parse(this.DefaultRequestHeaders.Where(h => h.Key == "limit").First().Value.First());
            set
            {
                this.DefaultRequestHeaders.Remove("limit"); this.DefaultRequestHeaders.Add("limit", value.ToString());
            }
        }

        private readonly Registry<Arguments> CommandRegistry;

        public bool HasCommands => CommandRegistry.Count > 0;

        public Task CommandAsync<TEntity>(CommandType command, TEntity payload, string name)
        {
            return Task.Factory.StartNew(() =>
            {
                Command(command, payload, name);
            });
        }

        public Task CommandSetAsync<TEntity>(
            CommandType command,
            IEnumerable<TEntity> payload,
            string name
        )
        {
            return Task.Factory.StartNew(() =>
            {
                CommandSet(command, payload, name);
            });
        }

        public void Command<TEntity>(CommandType command, TEntity payload, string name)
        {
            if (!CommandRegistry.TryGet(typeof(TEntity), out Arguments args))
            {
                args = new Arguments(name);
                args.Add(new DataArgument(name, payload, command.ToString(), typeof(TEntity).Name));
                CommandRegistry.Add(typeof(TEntity), args);
            }
            else
            {
                args.Put(new DataArgument(command.ToString(), payload, name, typeof(TEntity).Name));
            }
        }

        public void CommandSet<TEntity>(
            CommandType command,
            IEnumerable<TEntity> payload,
            string name
        )
        {
            if (!CommandRegistry.TryGet(typeof(TEntity), out Arguments args))
            {
                args = new Arguments(name);
                args.Add(
                    new DataArgument(
                        name,
                        payload.ToArray(),
                        command.ToString(),
                        typeof(TEntity).Name
                    )
                );
                CommandRegistry.Add(typeof(TEntity), args);
            }
            else
            {
                args.Put(new DataArgument(command.ToString(), payload, name, typeof(TEntity).Name));
            }
        }

        public Task CommandAsync(CommandType command, object payload, string name)
        {
            return Task.Factory.StartNew(() =>
            {
                Command(command, payload, name);
            });
        }

        public Task CommandSetAsync(
            CommandType command,
            IEnumerable<object> payload,
            string name
        )
        {
            return Task.Factory.StartNew(() =>
            {
                CommandSet(command, payload, name);
            });
        }

        public void Command(CommandType command, object payload, string name)
        {
            var type = payload?.GetType();
            if (!CommandRegistry.TryGet(type, out Arguments args))
            {
                args = new Arguments(name);
                args.Add(new DataArgument(name, payload, command.ToString(), type.Name));
                CommandRegistry.Add(type, args);
            }
            else
            {
                args.Put(new DataArgument(command.ToString(), payload, name, type.Name));
            }
        }

        public void CommandSet(
            CommandType command,
            IEnumerable<object> payload,
            string name
        )
        {
            var type = payload.FirstOrDefault()?.GetType();
            if (!CommandRegistry.TryGet(type, out Arguments args))
            {
                args = new Arguments(name);
                args.Add(
                    new DataArgument(
                        name,
                        payload.ToArray(),
                        command.ToString(),
                        type.Name
                    )
                );
                CommandRegistry.Add(type, args);
            }
            else
            {
                args.Put(new DataArgument(command.ToString(), payload, name, type.Name));
            }
        }

        protected async Task<string[]> CommandSetHandler()
        {
            var messages = new Registry<string>();

            await using (CommandRegistry)
            {
                foreach (var cmdType in CommandRegistry)
                {
                    foreach (var cmdMethod in cmdType.Value.GroupBy(method => method.MethodName))
                    {
                        var args = cmdMethod.Select(arg => (DataArgument)arg).ToArray();
                        var response = await CommandRequest(
                                                        cmdMethod.Key,
                                                        cmdType.Value.TargetName,
                                                        new DataContent(args)
                                                    );

                        var message = await GetResponseMessage(response);

                        messages.Add(message);
                    }
                }
            }

            return messages.ToArray();
        }

        protected async Task<string[]> CommandHandler()
        {
            var messages = new List<string>();

            await using (CommandRegistry)
            {
                foreach (var cmdType in CommandRegistry)
                {
                    foreach (var cmdMethod in cmdType.Value.GroupBy(method => method.MethodName))
                    {
                        foreach (var arg in cmdMethod)
                        {
                            var response = await CommandRequest(
                                                            cmdMethod.Key,
                                                            cmdType.Value.TargetName,
                                                            new DataContent((DataArgument)arg)
                                                        );

                            var message = await GetResponseMessage(response);

                            messages.Add(message);
                        }
                    }
                }
            }

            return messages.ToArray();
        }

        protected async Task<HttpResponseMessage> CommandRequest(
            string command,
            string name,
            DataContent content
        )
        {
            if (Enum.TryParse<CommandType>(command, out CommandType commandEnum))
            {
                switch (commandEnum)
                {
                    case CommandType.POST:
                        return await PostAsync(name, content);
                    case CommandType.PATCH:
                        return await PatchAsync(name, content);
                    case CommandType.DELETE:
                        return await DeleteAsync(name, content);
                    case CommandType.PUT:
                        return await PutAsync(name, content);
                    default:
                        break;
                }
            }
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Unsupported commend")
            };
        }

        public async Task<string[]> SendCommands(bool changesets)
        {
            if (!HasCommands)
                return default;

            if (changesets)
                return await CommandSetHandler();
            else
                return await CommandHandler();
        }

        private Registry<Arguments> GetProcessRegistry()
        {
            return new Registry<Arguments>(
            CommandRegistry.ForEach(typeArgs =>

               new SeriesItem<Arguments>(typeArgs.TargetType, new Arguments(typeArgs.TargetType) { typeArgs.ForEach(arg => typeArgs.Remove(arg.Id)) })
            ));            
        }

        private async Task<string> GetResponseMessage(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var message = $"StatusCode:{response.StatusCode.ToString()} Message:{content}";
            if ((int)response.StatusCode > 210)
                this.Info<Apilog>(message);
            else
                this.Info<Apilog>(message);
            return message;
        }

        public async Task<int> Count<TContract>()
        {
            var response = await this.GetAsync("count");
            return Convert.ToInt32(await response.Content.ReadAsStringAsync());
        }

        public async Task<IEnumerable<TContract>> Get<TContract>(int page = 0, int limit = 0) where TContract : IOrigin, IInnerProxy
        {
            SetPagination(page, limit);
            return await this.GetFromJsonAsync<IEnumerable<TContract>>(typeof(TContract).Name);
        }
        public async Task<TContract> Get<TContract>(object key) where TContract : IOrigin, IInnerProxy
        {
            return await this.GetFromJsonAsync<TContract>($"{typeof(TContract).Name}/{key.ToString()}");
        }

        public async Task<string> Create<TContract>(object key, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PostAsJsonAsync<TContract>($"{typeof(TContract).Name}/{key.ToString()}", contract)).Content.ReadAsStringAsync();
        }
        public async Task<string> Create<TContract>(TContract[] contracts) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PostAsJsonAsync<TContract[]>($"{typeof(TContract).Name}", contracts)).Content.ReadAsStringAsync();
        }

        public async Task<string> Change<TContract>(object key, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PatchAsJsonAsync<TContract>($"{typeof(TContract).Name}/{key.ToString()}", contract)).Content.ReadAsStringAsync();
        }
        public async Task<string> Change<TContract>(TContract[] contracts) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PatchAsJsonAsync<TContract[]>($"{typeof(TContract).Name}", contracts)).Content.ReadAsStringAsync();
        }

        public async Task<string> Update<TContract>(object key, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PutAsJsonAsync<TContract>($"{typeof(TContract).Name}/{key.ToString()}", contract)).Content.ReadAsStringAsync();
        }
        public async Task<string> Update<TContract>(TContract[] contracts) where TContract : IOrigin, IInnerProxy
        {
            return await (await this.PutAsJsonAsync<TContract[]>($"{typeof(TContract).Name}", contracts)).Content.ReadAsStringAsync();
        }

        public async Task<string> Delete<TContract>(object key, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{typeof(TContract).Name}/{key.ToString()}");
            request.Content = new ByteArrayContent(contract.ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadAsStringAsync();
        }
        public async Task<string> Delete<TContract>(TContract[] contracts) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{typeof(TContract).Name}");
            request.Content = new ByteArrayContent(contracts.ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadAsStringAsync();
        }

        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, HttpContent content)
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
            request.Content = content;
            return await this.SendAsync(request);
        }

        public async Task<TContract> Action<TContract>(string method, Arguments arguments) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Action/{method}");
            request.Content = new ByteArrayContent(arguments.ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TContract>();
        }
        public async Task<TResult> Action<TContract, TResult>(string method, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Action/{method}");
            request.Content = new ByteArrayContent(new Arguments(method, contract).ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TResult>();
        }

        public async Task<TContract> Access<TContract>(string method, Arguments arguments) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Service/{method}");
            request.Content = new ByteArrayContent(arguments.ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TContract>();
        }
        public async Task<TResult> Access<TContract, TResult>(string method, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Service/{method}");
            request.Content = new ByteArrayContent(new Arguments(method, contract).ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TResult>();
        }

        public async Task<TContract> Setup<TContract>(string method, Arguments arguments) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Setup/{method}");
            request.Content = new ByteArrayContent(arguments.ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TContract>();
        }
        public async Task<TResult> Setup<TContract, TResult>(string method, TContract contract) where TContract : IOrigin, IInnerProxy
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{typeof(TContract).Name}/Setup/{method}");
            request.Content = new ByteArrayContent(new Arguments(method, contract).ToJsonBytes());
            return await (await this.SendAsync(request)).Content.ReadFromJsonAsync<TResult>();
        }
    }

    public enum CommandType
    {
        POST,
        PATCH,
        PUT,
        DELETE
    }
}