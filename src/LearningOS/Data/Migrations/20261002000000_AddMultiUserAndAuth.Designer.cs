using System;
using LearningOS.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningOS.Data.Migrations
{
    [DbContext(typeof(LearningDbContext))]
    [Migration("20261002000000_AddMultiUserAndAuth")]
    partial class AddMultiUserAndAuth
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.11");
#pragma warning restore 612, 618
        }
    }
}
