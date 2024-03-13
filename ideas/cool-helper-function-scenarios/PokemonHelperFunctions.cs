//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Azure.AI.Details.Common.CLI.Extensions.HelperFunctions;

public static class PokemonHelperFunctions
{
    private static readonly HttpClient client = new HttpClient();

    [HelperFunctionDescription("Get information of a pokemon")]
    public static async Task<string> GetPokemonInfo(string pokemonName)
    {
        var responseString = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{pokemonName}");
        return responseString;
    }

    [HelperFunctionDescription("Get abilities of a pokemon")]
    public static async Task<List<string>> GetPokemonAbilities(string pokemonName)
    {
        var responseString = await client.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{pokemonName}");
        dynamic responseObj = JsonConvert.DeserializeObject(responseString);
        List<string> abilities = new List<string>();
        foreach (var ability in responseObj.abilities)
        {
            abilities.Add(Convert.ToString(ability.ability.name));
        }
        return abilities;
    }
}