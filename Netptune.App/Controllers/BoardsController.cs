﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Netptune.Core.Entities;
using Netptune.Core.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netptune.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BoardsController : ControllerBase
    {
        private readonly IBoardService BoardService;

        public BoardsController(IBoardService boardService)
        {
            BoardService = boardService;
        }

        // GET: api/boards
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Produces("application/json", Type = typeof(List<Board>))]
        public async Task<IActionResult> GetBoards(int projectId)
        {
            var result = await BoardService.GetBoards(projectId);

            return Ok(result);
        }

        // GET: api/boards/5
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Board))]
        public async Task<IActionResult> GetBoard([FromRoute] int id)
        {
            var result = await BoardService.GetBoard(id);

            return Ok(result);
        }

        // PUT: api/boards/5
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Board))]
        public async Task<IActionResult> PutBoard([FromBody] Board board)
        {
            var result = await BoardService.UpdateBoard(board);

            return Ok(result);
        }

        // POST: api/boards
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces("application/json", Type = typeof(Board))]
        public async Task<IActionResult> PostBoard([FromBody] Board board)
        {
            var result = await BoardService.AddBoard(board);

            return Ok(result);
        }

        // DELETE: api/ProjectBoards/5
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json", Type = typeof(Board))]
        public async Task<IActionResult> DeleteBoard([FromRoute] int id)
        {
            var result = await BoardService.DeleteBoard(id);

            return Ok(result);
        }
    }
}
