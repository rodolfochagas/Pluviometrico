using Microsoft.AspNetCore.Mvc;
using Pluviometrico.Core.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Pluviometrico.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MeasuredRainfallController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public MeasuredRainfallController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //Retorna apenas os 10 primeiros?
        //TODO: Retornar status de erro quando der erro
        //TODO: padronizar controller
        //TODO: paging
        [HttpGet("ano/{year:int}")]
        public async Task<IActionResult> GetByDate(int year)
        {
            var response = await _unitOfWork.FilterByYear(year);
            return Ok(response);
        }

        [HttpGet("indice/{index:double}")]
        public async Task<IActionResult> GetByRainfallIndex(double index)
        {
            var response = await _unitOfWork.FilterByRainfallIndex(index);
            return Ok(response);
        }

        [HttpGet("distancia")]
        public async Task<IActionResult> FilterByDistance([FromQuery] double distance)
        {
            var response = await _unitOfWork.FilterByDistance(distance);
            return Ok(response);
        }

        [HttpGet("distancia/indice")]
        public async Task<IActionResult> FilterByDistanceAndRainfallIndex([FromQuery] double distance, [FromQuery] double index)
        {
            var response = await _unitOfWork.FilterByDistanceAndRainfallIndex(distance, index);
            return Ok(response);
        }

        [HttpGet("distancia/data")]
        public async Task<IActionResult> FilterByDistanceAndDate([FromQuery] double distance, [FromQuery] int year, [FromQuery] int month, [FromQuery] int day)
        {
            var response = await _unitOfWork.FilterByDistanceAndDate(distance, year, month, day);
            return Ok(response);
        }

        [HttpGet("distancia/data-intervalo")]
        public async Task<IActionResult> FilterByDistanceAndDateRange([FromQuery] double distance, [FromQuery] DateTime firstDate, [FromQuery] DateTime secondDate)
        {
            var response = await _unitOfWork.FilterByDistanceAndDateRange(firstDate, secondDate, distance);
            return Ok(response);
        }

        [HttpGet("distancia/cidade")]
        public async Task<IActionResult> FilterByDistanceAndCity([FromQuery] double distance, [FromQuery] string city, [FromQuery] int limit)
        {
            var response = await _unitOfWork.FilterByDistanceAndCity(distance, city, limit);
            return Ok(response);
        }

        [HttpGet("media/cidade")]
        public async Task<IActionResult> GetAverageRainfallIndexByCity([FromQuery] string city, [FromQuery] int limit)
        {
            var response = await _unitOfWork.GetAverageRainfallIndexByCity(city, limit);
            return Ok(response);
        }

        [HttpGet("gelocalizacao/cidade")]
        public async Task<IActionResult> FilterByGeolocationAndCity(
            [FromQuery] string city,
            [FromQuery] double minLatitude, [FromQuery] double maxLatitude,
            [FromQuery] double minLongitude, [FromQuery] double maxLongitude
        )
        {
            var response = await _unitOfWork.FilterByGeolocationAndCity(city, minLatitude, maxLatitude, minLongitude, maxLongitude);
            return Ok(response);
        }

        [HttpGet("gelocalizacao/data-intervalo")]
        public async Task<IActionResult> FilterByGeolocationAndDateRange(
            [FromQuery] DateTime firstDate, [FromQuery] DateTime secondDate,
            [FromQuery] double minLatitude, [FromQuery] double maxLatitude,
            [FromQuery] double minLongitude, [FromQuery] double maxLongitude
        )
        {
            var response = await _unitOfWork.FilterByGeolocationAndDateRange(firstDate, secondDate, minLatitude, maxLatitude, minLongitude, maxLongitude);
            return Ok(response);
        }

        [HttpGet("gelocalizacao/indice")]
        public async Task<IActionResult> FilterByGeolocationAndRainfallIndex(
            [FromQuery] double index,
            [FromQuery] double minLatitude, [FromQuery] double maxLatitude,
            [FromQuery] double minLongitude, [FromQuery] double maxLongitude
        )
        {
            var response = await _unitOfWork.FilterByGeolocationAndRainfallIndex(index, minLatitude, maxLatitude, minLongitude, maxLongitude);
            return Ok(response);
        }
    }
}
