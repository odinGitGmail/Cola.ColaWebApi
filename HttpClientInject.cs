﻿using System.Net;
using System.Security.Cryptography.X509Certificates;
using Cola.Core.ColaConsole;
using Cola.Core.ColaException;
using Cola.Core.Models.ColaWebApi;
using Cola.Core.Utils.Constants;
using Cola.CoreUtils.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cola.ColaWebApi;

public static class HttpClientInject
{
    /// <summary>
    ///     inject HttpClient 
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="config">config</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddColaHttpClient(this IServiceCollection services, IConfiguration config)
    {
        var clientConfigs = config.GetColaSection<List<ClientConfig>>(SystemConstant.CONSTANT_COLAWEBAPI_SECTION);
        return InjectColaHttpClient(services, config, clientConfigs);
    }

    /// <summary>
    ///     inject SnowFlake
    /// </summary>
    /// <param name="services">IServiceCollection</param>
    /// <param name="config">action</param>
    /// <param name="action">action</param>
    /// <returns>IServiceCollection</returns>
    public static IServiceCollection AddColaHttpClient(this IServiceCollection services, IConfiguration config,
        Action<WebApiOption> action)
    {
        var opts = new WebApiOption();
        action(opts);
        return InjectColaHttpClient(services, config, opts.ClientConfigs);
    }

    private static IServiceCollection InjectColaHttpClient(IServiceCollection services, IConfiguration config,
        List<ClientConfig> clientConfigs)
    {
        var exceptionHelper = services.BuildServiceProvider().GetService<IColaException>();
        var defaultSettings =
            config.GetColaSection<ColaWebApiSettings>(SystemConstant.CONSTANT_COLAWEBAPI_DEFAULTSETTINGS_SECTION)
            ??
            new ColaWebApiSettings();
        ValidateConfig(services, defaultSettings, clientConfigs);
        var env = services.BuildServiceProvider().GetService<IWebHostEnvironment>();
        foreach (var clientConfig in clientConfigs)
        {
            exceptionHelper!.ThrowStringIsNullOrEmpty(clientConfig.ClientName, "ClientName");
            services.AddHttpClient(clientConfig.ClientName, c =>
            {
                c.Timeout = TimeSpan.FromMilliseconds(clientConfig.TimeOut);
                if (!clientConfig.BaseUri.StringIsNullOrEmpty())
                    c.BaseAddress = new Uri(clientConfig.BaseUri!);
            }).ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (exceptionHelper.ThrowIfNull(clientConfig.Cert) == null &&
                    !clientConfig.Cert!.CertFilePath.StringIsNullOrEmpty())
                    handler.ClientCertificates.Add(new X509Certificate2(
                        Path.Combine(env!.ContentRootPath, clientConfig.Cert!.CertFilePath),
                        clientConfig.Cert!.CertFilePwd));
                SetRequestHeaders(handler, clientConfig, defaultSettings);
                return handler;
            });
            ConsoleHelper.WriteInfo(
                $"注入类型【 HttpClient 】\tname: {clientConfig.ClientName}\t{(clientConfig.BaseUri.StringIsNullOrEmpty() ? "" : $"baseUri:{clientConfig.BaseUri}")}");
        }

        return services;
    }

    private static void ValidateConfig(IServiceCollection services, ColaWebApiSettings defaultSettings,
        List<ClientConfig> clientConfigs)
    {
        var exceptionHelper = services.BuildServiceProvider().GetService<IColaException>();

        #region 自定义配置文件检查

        if (clientConfigs.Any(c => c.ClientName.StringCompareIgnoreCase(defaultSettings.ForbiddenClientName)))
            exceptionHelper!.ThrowException(
                $"HttpClient 自定义配置文件 ClientName 不能命名为 {defaultSettings.ForbiddenClientName}");
        if (clientConfigs.Any(c =>
                !c.Decompression.StringIsNullOrEmpty() && !SystemConstant.CONSTANT_COLAWEBAPI_DECOMPRESSION_SECTION
                    .Split(',')
                    .Contains(c.Decompression)))
            exceptionHelper!.ThrowException(
                "HttpClient 自定义配置文件 Decompression 只可以配置为 None,GZip,Deflate,Brotli,All 其中的一项");

        #endregion


        #region 默认配置文件检查

        if (!defaultSettings.ForbiddenClientName.StringCompareIgnoreCase(SystemConstant
                .CONSTANT_COLAWEBAPI_FORBIDDENCLIENTNAME_SECTION))
            exceptionHelper!.ThrowException(
                $"HttpClient默认配置文件 ClientName 只可以命名为 {SystemConstant.CONSTANT_COLAWEBAPI_FORBIDDENCLIENTNAME_SECTION}");
        if (!SystemConstant.CONSTANT_COLAWEBAPI_DECOMPRESSION_SECTION.Split(',')
                .Contains(defaultSettings.Decompression))
            exceptionHelper!.ThrowException("HttpClient默认配置文件 Decompression 只可以配置为 None,GZip,Deflate,Brotli,All 其中的一项");

        #endregion
    }

    private static void SetRequestHeaders(HttpClientHandler httpClientHandler, ClientConfig clientConfig,
        ColaWebApiSettings defaultSettings)
    {
        httpClientHandler.AutomaticDecompression = clientConfig.Decompression.StringIsNullOrEmpty()
            ? defaultSettings.Decompression.ConvertStringToEnum<DecompressionMethods>()
            : clientConfig.Decompression!.ConvertStringToEnum<DecompressionMethods>();
    }
}