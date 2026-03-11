using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FoodHub.Application.Extensions.Mappings;
using FoodHub.Domain.Entities;

namespace FoodHub.Application.Features.MergeSplitOrder.Commands.ChangeOrderTable
{
    public class ChangeOrderTableValidator : IMapFrom<Order>
    {
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Order, ChangeOrderTableValidator>();
        }
    }
}
