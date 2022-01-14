﻿using Nest;
using Pluviometrico.Core.DTOs;
using Pluviometrico.Core.Repository.Interface;
using Pluviometrico.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pluviometrico.Core.Repository
{
    public class MeasuredRainfallRepositoryES : IMeasuredRainfallRepository
    {
        private readonly IElasticClient _elasticClient;
        private readonly string _distanceCalculationString = "6371 * Math.acos(Math.cos(-22.913924*Math.PI/180) * Math.cos(doc['latitude'].value*Math.PI/180) * Math.cos(-43.084737*Math.PI/180 - (doc['longitude'].value*Math.PI/180)) + Math.sin(-22.913924*Math.PI/180) * Math.sin(doc['latitude'].value*Math.PI/180))";

        public MeasuredRainfallRepositoryES(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }
        public async Task<List<MeasuredRainfallDTO>> FilterByYear(int year)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s =>
                s.Query(q =>
                    q.Bool(b => 
                        b.Must(m =>
                            m.Term(t => t.Field(f => f.Ano).Value(year))
                        )
                    )
                )
            );

            return response?.Documents?.Select(d => new MeasuredRainfallDTO
            {
                Source = "CEMADEN",
                City = d.Municipio,
                UF = d.UF,
                Day = d.Dia,
                Month = d.Mes,
                Year = d.Ano,
                Hour = d.Hora,
                StationCode = d.CodEstacaoOriginal,
                StationName = d.NomeEstacaoOriginal,
                RainfallIndex = d.ValorMedida
            }).ToList();
        }

        public async Task<List<MeasuredRainfallDTO>> FilterByRainfallIndex(double index)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s =>
                s.Query(q =>
                    q.Bool(b =>
                        b.Must(m =>
                            m.Range(r => r.Field(f => f.ValorMedida).GreaterThan(index))
                        )
                    )
                )
            );

            return response?.Documents?.Select(d => new MeasuredRainfallDTO
            {
                Source = "CEMADEN",
                City = d.Municipio,
                UF = d.UF,
                Day = d.Dia,
                Month = d.Mes,
                Year = d.Ano,
                Hour = d.Hora,
                StationCode = d.CodEstacaoOriginal,
                StationName = d.NomeEstacaoOriginal,
                RainfallIndex = d.ValorMedida
            }).ToList();
        }

        public async Task<List<object>> FilterByDistance(double distance)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(s => s.ScriptField("distancia", script => script
                    .Source(_distanceCalculationString)
                ))
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => f
                            .Script(s => s
                                .Script(s => s
                                    .Source($"double distancia = {_distanceCalculationString}; return distancia < {distance};"))))))
            );
            var filteredResponse = new List<object>();

            foreach (var hit in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    source = hit.Source,
                    distancia = hit.Fields.Value<double>("distancia")
                });
            }

            return filteredResponse;
        }

        public async Task<List<object>> FilterByDistanceAndRainfallIndex(double distance, double index)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(s => s.ScriptField("distancia", script => script
                    .Source(_distanceCalculationString)
                ))
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => f
                            .Script(s => s
                                .Script(s => s
                                    .Source($"double distancia = {_distanceCalculationString}; return distancia > {distance};"))))
                        .Must(m => m.Range(r => r.Field(f => f.ValorMedida).LessThan(index)))
                        ))
            );
            var filteredResponse = new List<object>();

            foreach (var hit in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    source = hit.Source,
                    distancia = hit.Fields.Value<double>("distancia")
                });
            }

            return filteredResponse;
        }

        public async Task<List<object>> FilterByDistanceAndDate(double distance, int year, int month, int day)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(s => s.ScriptField("distancia", script => script
                    .Source(_distanceCalculationString)
                ))
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => f
                            .Script(s => s
                                .Script(s => s
                                    .Source($"double distancia = {_distanceCalculationString}; return distancia < {distance};"))))
                        .Must(m => 
                            m.Term(t => t.Field(f => f.Ano).Value(year)) &&
                            m.Term(t => t.Field(f => f.Mes).Value(month)) &&
                            m.Term(t => t.Field(f => f.Dia).Value(day))
                        )))
            );
            var filteredResponse = new List<object>();

            foreach (var hit in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    source = hit.Source,
                    distancia = hit.Fields.Value<double>("distancia")
                });
            }

            return filteredResponse;
        }

        //TODO: Check if adding "distancia" field significantly slows response time"?
        public async Task<List<object>> FilterByDistanceAndYearRange(int greaterThanYear, int lessThanYear, double distance)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(sf =>
                    sf.ScriptField("distancia", script => script
                        .Source(_distanceCalculationString)))
                .Query(q =>
                    q.Bool(b => b
                        .Filter(f => f.Script(s => s.Script(s => s.Source($"double distancia = {_distanceCalculationString} ; return distancia < {distance};"))))
                        .Must(m => m.Range(r => r.Field(f => f.Ano).GreaterThanOrEquals(greaterThanYear).LessThanOrEquals(lessThanYear))))));

            var filteredResponse = new List<object>();

            foreach(var h in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    Source = h.Source,
                    Distancia = h.Fields.Value<double>("distancia")
                });
            }
            return filteredResponse;
        }

        public async Task<List<object>> FilterByDistanceAndDateRange(DateTime firstDate, DateTime secondDate, double distance)
        {
            var dates = Utils.MaxMinDate(firstDate, secondDate);

            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(sf => sf
                    .ScriptField("distancia", script => script
                        .Source(_distanceCalculationString)))
                .Query(q => q.Bool(b => b
                    .Filter(f => f.Script(s => s.Script(s => s.Source($"double distancia = {_distanceCalculationString} ; return distancia < {distance};"))))
                    .Must(m => m.DateRange(r => r.Field(f => f.DataHora).GreaterThanOrEquals(DateMath.Anchored(dates.lesserDate)).LessThanOrEquals(DateMath.Anchored(dates.greaterDate)))))
            ));

            var filteredResponse = new List<object>();

            foreach (var h in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    Source = h.Source,
                    Distancia = h.Fields.Value<double>("distancia")
                });
            }
            return filteredResponse;
        }

        public async Task<List<object>> FilterByDistanceAndCity(double distance, string city, int limit)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(sf =>
                    sf.ScriptField("distancia", script => script
                        .Source(_distanceCalculationString)))
                .Query(q =>
                    q.Bool(b => b
                        .Filter(f => f.Script(s => s.Script(s => s.Source($"double distancia = {_distanceCalculationString} ; return distancia > {distance};"))))
                        .Must(m => m.Match(t => t.Field(f => f.Municipio).Query(city))))));

            var filteredResponse = new HashSet<object>();

            foreach (var h in response?.Hits)
            {
                filteredResponse.Add(Utils.FormattedResponse(h.Source, h.Fields.Value<double>("distancia")));
            }
            return filteredResponse.Take(limit).ToList();

        }

        public async Task<List<MeasuredRainfallDTO>> GetAverageRainfallIndexByCity(string city, int limit)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .RuntimeFields<MeasuredRainfallRuntimeFields>(r => r
                    .RuntimeField(r => r.Distancia, FieldType.Double, r => r
                        .Script($"double distancia = {_distanceCalculationString}; emit(distancia);")))
                .Aggregations(a => a
                    .Terms("codigoEstacao", t => t
                        .Field(f => f.CodEstacaoOriginal.Suffix("keyword"))
                        .Aggregations(a => a
                            .Terms("estacao", t => t
                                .Field(f => f.NomeEstacaoOriginal.Suffix("keyword"))
                                .Aggregations(a => a
                                    .Terms("municipio", t => t
                                        .Field(f => f.Municipio.Suffix("keyword"))
                                        .Aggregations(a => a
                                            .Terms("UF", t => t
                                                .Field(f => f.UF.Suffix("keyword"))
                                                .Aggregations(a => a
                                                    .Terms("distancia", t => t
                                                        .Field("distancia")
                                                        .Aggregations(a => a
                                                            .Average("media", s => s
                                                                .Field(f => f.ValorMedida)))))))))))))
                .Query(q => q.Bool(b => b.Must(m =>
                    m.Match(t => t
                        .Field(f => f.Municipio).Query(city))
                )))
            );

            var filteredResponse = new List<MeasuredRainfallDTO>();

            var stationCodeBuckets = response.Aggregations.Terms("codigoEstacao").Buckets;
            foreach (var stationCodeBucket in stationCodeBuckets)
            {
                var stationCode = stationCodeBucket.Key;
                var stationBuckets = stationCodeBucket.Terms("estacao").Buckets;
                foreach (var stationBucket in stationBuckets)
                {
                    var station = stationBucket.Key;
                    var cityBuckets = stationBucket.Terms("municipio").Buckets;
                    foreach (var cityBucket in cityBuckets)
                    {
                        var responseCity = cityBucket.Key;
                        var UFBuckets = cityBucket.Terms("UF").Buckets;
                        foreach (var UFBucket in UFBuckets)
                        {
                            var uF = UFBucket.Key;
                            var distanceBuckets = UFBucket.Terms("distancia").Buckets;
                            foreach (var distanceBucket in distanceBuckets)
                            {
                                var responseDistance = double.Parse(distanceBucket.Key);
                                var average = distanceBucket.Average("media").Value;
                                filteredResponse.Add(new MeasuredRainfallDTO
                                {
                                    Source = "CEMADEN",
                                    City = responseCity,
                                    UF = uF,
                                    StationCode = stationCode,
                                    StationName = station,
                                    Distance = responseDistance,
                                    AverageRainfallIndex = average
                                });
                            }
                        }
                    }
                }
            }

            return filteredResponse.Take(limit).Distinct().ToList();
        }
















        public async Task<List<object>> FilterByDistanceAndYear(int year, double distance)
        {
            var distanceCalculationString = "6371 * Math.acos(Math.cos(-22.913924*Math.PI/180) * Math.cos(doc['latitude'].value*Math.PI/180) * Math.cos(-43.084737*Math.PI/180 - (doc['longitude'].value*Math.PI/180)) + Math.sin(-22.913924*Math.PI/180) * Math.sin(doc['latitude'].value*Math.PI/180))";

            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(sf =>
                    sf.ScriptField("distancia", script => script
                        .Source(distanceCalculationString)))
                .Query(q =>
                    q.Bool(b => b
                        .Filter(f => f.Script(s => s.Script(s => s.Source($"double distancia = {distanceCalculationString} ; return distancia < {distance};"))))
                        .Must(m => m.Term(t => t.Field(f => f.Ano).Value(year))))));

            var filteredResponse = new List<object>();

            foreach (var h in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    Source = h.Source,
                    Distancia = h.Fields.Value<double>("distancia")
                });
            }

            return filteredResponse;
        }

        public async Task<List<object>> GetMeasureByCityFilterByDate(int year)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Size(0)
                .Aggregations(a => a.Terms("municipio", t => t
                    .Field(f => f.Municipio.Suffix("keyword"))
                    //aggregations inside "municipio"
                    .Aggregations(a => a.Terms("mes", t => t
                        .Field(f => f.Mes)
                        .Aggregations(a => a.Sum("soma", s => s
                            .Field(f => f.ValorMedida)))))))
                .Query(q => q.Bool(b => b.Must(m => m.Term(t => t.Field(f => f.Ano).Value(year))))));

            var filteredResponse = new List<object>();

            var cityBuckets = response.Aggregations.Terms("municipio").Buckets;
            foreach (var cityBucket in cityBuckets)
            {
                var city = cityBucket.Key;
                var monthBuckets = cityBucket.Terms("mes").Buckets;
                foreach(var monthBucket in monthBuckets)
                {
                    var month = monthBucket.Key; 
                    var sumValueBuckets = monthBucket.Sum("soma").Value;
                    filteredResponse.Add(new {
                        municipio = city,
                        mes = month,
                        ano = year,
                        soma = sumValueBuckets
                    });
                }
            }
            return filteredResponse;
        }

        public async Task<List<object>> GetMeasureByCityFilterByYearAndDistance(int year, double distance)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Size(0)
                .RuntimeFields<MeasuredRainfallRuntimeFields>(r => r
                    .RuntimeField(f => f.Distancia, FieldType.Double, f => f
                        .Script($"double distancia = {_distanceCalculationString}; emit(distancia);"))
                )
                .Aggregations(a => a
                    .Terms("municipio", t => t
                        .Field(f => f.Municipio.Suffix("keyword"))
                        .Aggregations(a => a
                            .Terms("mes", t => t
                                .Field(f => f.Mes)
                                .Aggregations(a => a
                                    .Terms("distancia", t => t
                                        .Field("distancia")
                                        .Aggregations(a => a
                                            .Sum("soma", s => s.Field(f => f.ValorMedida)))))))))
                .Query(q => q.Bool(b => b
                    .Must(m =>
                        m.Term(t => t.
                            Field(f => f.Ano)
                            .Value(year)
                         ) &&
                         m.Range(r => r
                            .Field("distancia")
                            .LessThan(distance)
                         )
                    )
                ))
            );

            var filteredResponse = new List<object>();

            var cityBuckets = response.Aggregations.Terms("municipio").Buckets;
            foreach (var cityBucket in cityBuckets)
            {
                var city = cityBucket.Key;
                var monthBuckets = cityBucket.Terms("mes").Buckets;
                foreach(var monthBucket in monthBuckets)
                {
                    var month = monthBucket.Key;
                    var distanceBuckets = monthBucket.Terms("distancia").Buckets;
                    foreach (var distanceBucket in distanceBuckets)
                    {
                        var responseDistance = distanceBucket.Key;
                        var sum = distanceBucket.Sum("soma").Value;
                        filteredResponse.Add(new
                        {
                            municipio = city,
                            mes = month,
                            soma = sum,
                            distancia = double.Parse(responseDistance)
                        });
                    }
                }
            }

            return filteredResponse;
        }

        //TODO: filtrar por mês também
        public async Task<List<object>> GetAverageMeasureByCityAndStationFilterByDateAndDistance(int year, double distance, int month)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .RuntimeFields<MeasuredRainfallRuntimeFields>(r => r
                    .RuntimeField(r => r.Distancia, FieldType.Double, r => r
                        .Script($"double distancia = {_distanceCalculationString}; emit(distancia);")))
                .Aggregations(a => a
                    .Terms("codigoEstacao", t => t
                        .Field(f => f.CodEstacaoOriginal.Suffix("keyword"))
                        .Aggregations(a => a
                            .Terms("estacao", t => t
                                .Field(f => f.NomeEstacaoOriginal.Suffix("keyword"))
                                .Aggregations(a => a
                                    .Terms("municipio", t => t
                                        .Field(f => f.Municipio.Suffix("keyword"))
                                        .Aggregations(a => a
                                            .Terms("mes", t => t
                                                .Field(f => f.Mes)
                                                .Aggregations(a => a
                                                    .Terms("ano", t => t
                                                        .Field(f => f.Ano)
                                                        .Aggregations(a => a
                                                            .Terms("distancia", t => t
                                                                .Field("distancia")
                                                                .Aggregations(a => a
                                                                    .Average("media", s => s
                                                                        .Field(f => f.ValorMedida)))))))))))))))
                .Query(q => q.Bool(b => b.Must(m =>
                    m.Term(t => t
                        .Field(f => f.Ano)
                        .Value(year)) &&
                    m.Range(r => r
                        .Field("distancia")
                        .LessThan(distance))
                )))
            );

            var filteredResponse = new List<object>();

            var stationCodeBuckets = response.Aggregations.Terms("codigoEstacao").Buckets;
            foreach(var stationCodeBucket in stationCodeBuckets)
            {
                var stationCode = stationCodeBucket.Key;
                var stationBuckets = stationCodeBucket.Terms("estacao").Buckets;
                foreach (var stationBucket in stationBuckets)
                {
                    var station = stationBucket.Key;
                    var cityBuckets = stationBucket.Terms("municipio").Buckets;
                    foreach (var cityBucket in cityBuckets)
                    {
                        var city = cityBucket.Key;
                        var monthBuckets = cityBucket.Terms("mes").Buckets;
                        foreach (var monthBucket in monthBuckets)
                        {
                            var responseMonth = monthBucket.Key;
                            var yearBuckets = monthBucket.Terms("ano").Buckets;
                            foreach (var yearBucket in yearBuckets)
                            {
                                var responseYear = yearBucket.Key;
                                var distanceBuckets = yearBucket.Terms("distancia").Buckets;
                                foreach (var distanceBucket in distanceBuckets)
                                {
                                    var responseDistance = distanceBucket.Key;
                                    var average = distanceBucket.Average("media").Value;
                                    filteredResponse.Add(new
                                    {
                                        codEstacaoOriginal = stationCode,
                                        nomeEstacaoOriginal = station,
                                        municipio = city,
                                        mes = responseMonth,
                                        ano = responseYear,
                                        distancia = responseDistance,
                                        media = average
                                    });
                                }
                            }

                        }

                    }
                }
            }

            return filteredResponse;
        }

        public async Task<List<MeasuredRainfall>> GetAll()
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s.Source(true));
            return response.Documents.ToList();
        }


        public async Task<List<object>> GetAllWithDistance()
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Source(true)
                .ScriptFields(s => s
                    .ScriptField("distancia", script => script
                        .Source(_distanceCalculationString)
                    )
                )
            );

            var filteredResponse = new List<object>();

            foreach (var hit in response?.Hits)
            {
                filteredResponse.Add(new
                {
                    source = hit.Source,
                    distancia = hit.Fields.Value<double>("distancia")
                });
            }

            return filteredResponse;
        }

        public async Task<List<object>> GetMeasureByCityAndYear()
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s.Aggregations(a => a
                .Terms("municipio", t => t
                    .Field(f => f.Municipio.Suffix("keyword"))
                    .Aggregations(a => a
                        .Terms("ano", t => t
                            .Field(f => f.Ano)
                            .Aggregations(a => a
                                .Sum("soma", s => s
                                    .Field(f => f.ValorMedida))))))
            ));
            var filteredResponse = new List<object>();

            var cityBuckets = response.Aggregations.Terms("municipio").Buckets;
            foreach (var cityBucket in cityBuckets)
            {
                var city = cityBucket.Key;
                var yearBuckets = cityBucket.Terms("ano").Buckets;
                foreach (var yearBucket in yearBuckets)
                {
                    var year = yearBucket.Key;
                    var sum = yearBucket.Sum("soma").Value;
                    filteredResponse.Add(new
                    {
                        municipio = city,
                        ano = year,
                        soma = sum
                    });
                }
            }

            return filteredResponse;
        }

        public async Task<List<object>> GetMeasureByCityAndYearFilterByDistance(double distance)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Size(0)
                .RuntimeFields(r => r.RuntimeField("distancia", FieldType.Double, f => f
                    .Script($"double distancia = { _distanceCalculationString}; emit(distancia);")))
                .Aggregations(a => a
                    .Terms("codEstacao", t => t
                        .Field(f => f.CodEstacaoOriginal.Suffix("keyword"))
                        .Aggregations(a => a
                            .Terms("estacao", t => t
                                .Field(f => f.NomeEstacaoOriginal.Suffix("keyword"))
                                .Aggregations(a => a
                                    .Terms("distancia", t => t
                                        .Field("distancia")
                                        .Aggregations(a => a
                                            .Sum("soma", s => s
                                                .Field(f => f.ValorMedida)))))))))
                .Query(q => q.Bool(b => b.Must(m => m.Range(r => r.Field("distancia").LessThan(distance))))));

            var filteredResponse = new List<object>();

            var stationCodeBuckets = response.Aggregations.Terms("codEstacao").Buckets;
            foreach (var stationCodeBucket in stationCodeBuckets)
            {
                var stationCode = stationCodeBucket.Key;
                var stationBuckets = stationCodeBucket.Terms("estacao").Buckets;
                foreach (var stationBucket in stationBuckets)
                {
                    var station = stationBucket.Key;
                    var distanceBuckets = stationBucket.Terms("distancia").Buckets;
                    foreach (var distanceBucket in distanceBuckets)
                    {
                        var responseDistance = distanceBucket.Key;
                        var sum = distanceBucket.Sum("soma").Value;
                        filteredResponse.Add(new {
                            codEstacaoOriginal = stationCode,
                            nomeEstacaoOriginal = station,
                            distancia = responseDistance,
                            soma = sum
                        });
                    }
                }
            }

            return filteredResponse;
        }

        public async Task<List<object>> GetMeasureByCityAndDateFilterByDistance(double distance)
        {
            var response = await _elasticClient.SearchAsync<MeasuredRainfall>(s => s
                .Size(0)
                .RuntimeFields(r => r.RuntimeField("distancia", FieldType.Double, r => r
                    .Script($"double distancia = { _distanceCalculationString}; emit(distancia);")
                ))
                .Aggregations(a => a
                    .Terms("mes", t => t
                        .Field(f => f.Mes)
                        .Aggregations(a => a
                            .Terms("ano", t => t
                                .Field(f => f.Ano)
                                .Aggregations(a => a
                                    .Terms("municipio", t => t
                                        .Field(f => f.Municipio.Suffix("keyword"))
                                        .Aggregations(a => a
                                            .Terms("distancia", t => t
                                                .Field("distancia")
                                                .Aggregations(a => a
                                                    .Sum("soma", s => s.Field(f => f.ValorMedida)))
                 ))))))))
                .Query(q => q.Bool(b => b.Must(m => m.Range(r => r.Field("distancia").LessThan(distance)))))
            );

            var filteredResponse = new List<object>();

            var monthBuckets = response.Aggregations.Terms("mes").Buckets;
            foreach (var monthBucket in monthBuckets)
            {
                var month = monthBucket.Key;
                var yearBuckets = monthBucket.Terms("ano").Buckets;
                foreach(var yearBucket in yearBuckets)
                {
                    var year = yearBucket.Key;
                    var cityBuckets = yearBucket.Terms("municipio").Buckets;
                    foreach (var cityBucket in cityBuckets)
                    {
                        var city = cityBucket.Key;
                        var distanceBuckets = cityBucket.Terms("distancia").Buckets;
                        foreach (var distanceBucket in distanceBuckets)
                        {
                            var responseDistance = distanceBucket.Key;
                            var sum = distanceBucket.Sum("soma").Value;
                            filteredResponse.Add(new
                            {
                                mes = month,
                                ano = year,
                                municipio = city,
                                distancia = responseDistance,
                                soma = sum
                            }
                            );
                        }
                    }
                }
            }

            return filteredResponse;
        }
    }
}
