﻿using Nest;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pluviometrico.Data
{
    [Table("dados_chuva_cemaden")] //para o PostgreSQL
    public class MeasuredRainfall
    {
        [PropertyName("id")]    //para o ElasticSearch
        [Column("id")]          //para o PostgreSQL
        public int Id { get; set; }

        [PropertyName("municipio")]
        [Column("municipio")]
        public string Municipio { get; set; }

        [PropertyName("cod_estacao_original")]
        [Column("cod_estacao_original")]
        public string CodEstacaoOriginal { get; set; }

        [PropertyName("uf")]
        [Column("uf")]
        public string UF { get; set; }

        [PropertyName("nome_estacao_original")]
        [Column("nome_estacao_original")]
        public string NomeEstacaoOriginal { get; set; }

        [PropertyName("latitude")]
        [Column("latitude")]
        public double Latitude { get; set; }

        [PropertyName("longitude")]
        [Column("longitude")]
        public double Longitude { get; set; }

        [PropertyName("datahora")]
        [Column("datahora")]
        public DateTime DataHora { get; set; }

        [PropertyName("valormedida")]
        [Column("valormedida")]
        public double ValorMedida { get; set; }

        [PropertyName("hora")]
        [Column("hora")]
        public int Hora { get; set; }

        [PropertyName("dia")]
        [Column("dia")]
        public int Dia { get; set; }

        [PropertyName("minuto")]
        [Column("minuto")]
        public int Minuto { get; set; }

        [PropertyName("mes")]
        [Column("mes")]
        public int Mes { get; set; }

        [PropertyName("ano")]
        [Column("ano")]
        public int Ano { get; set; }

        [PropertyName("datahora_ajustada")]
        [Column("datahora_ajustada")]
        public DateTime DataHoraAjustada { get; set; }

        [PropertyName("estado")]
        [Column("estado")]
        public string Estado { get; set; }

        [PropertyName("bairro")]
        [Column("bairro")]
        public string Bairro { get; set; }

        [PropertyName("cidade")]
        [Column("cidade")]
        public string Cidade { get; set; }
    }
}
