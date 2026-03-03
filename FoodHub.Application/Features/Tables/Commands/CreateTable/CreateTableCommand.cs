using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoodHub.Application.Common.Models;
using MediatR;

namespace FoodHub.Application.Features.Tables.Commands.CreateTable
{
    public record CreateTableCommand(
        int TableNumber,
        int Capacity,
        Guid AreaId
        ) : IRequest<Result<CreateTableResponse>>;
}
