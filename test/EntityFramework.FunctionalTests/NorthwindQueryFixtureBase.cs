// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Data.Entity.FunctionalTests.TestModels.Northwind;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.Data.Entity.FunctionalTests
{
    public abstract class NorthwindQueryFixtureBase
    {
        public Model CreateModel()
        {
            var modelBuilder = new BasicModelBuilder(new Model());
            OnModelCreating(modelBuilder);
            return modelBuilder.Model;
        }

        public virtual void OnModelCreating(BasicModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(e =>
                {
                    e.Key(c => c.CustomerID);
                    e.Property(c => c.CompanyName);
                    e.Property(c => c.ContactName);
                    e.Property(c => c.ContactTitle);
                    e.Property(c => c.Address);
                    e.Property(c => c.City);
                    e.Property(c => c.Region);
                    e.Property(c => c.PostalCode);
                    e.Property(c => c.Country);
                    e.Property(c => c.Phone);
                    e.Property(c => c.Fax);
                });

            modelBuilder.Entity<Employee>(e =>
                {
                    e.Key(em => em.EmployeeID);
                    e.Property(em => em.City);
                    e.Property(em => em.Country);
                    e.Property(em => em.FirstName);
                    e.Property(em => em.ReportsTo);
                    e.Property<string>("Title");
                });

            modelBuilder.Entity<Product>(e =>
                {
                    e.Key(p => p.ProductID);
                    e.Property(p => p.ProductName);
                    e.Property(p => p.Discontinued);
                });

            modelBuilder.Entity<Order>(e =>
                {
                    e.Key(o => o.OrderID);
                    e.Property(o => o.OrderDate);
                    e.ForeignKey<Customer>(o => o.CustomerID);
                });

            modelBuilder.Entity<OrderDetail>(e =>
                {
                    e.Key(od => new { od.OrderID, od.ProductID });
                    e.Property(od => od.UnitPrice);
                    e.Property(od => od.Quantity);
                    e.Property(od => od.Discount);
                    e.ForeignKey<Product>(od => od.ProductID);
                    e.ForeignKey<Order>(od => od.OrderID);
                });

            // TODO: Use FAPIS when avail.
            var model = modelBuilder.Model;
            var productType = model.GetEntityType(typeof(Product));
            var customerType = model.GetEntityType(typeof(Customer));
            var orderType = model.GetEntityType(typeof(Order));
            var orderDetailType = model.GetEntityType(typeof(OrderDetail));

            var customerIdFk = orderType.ForeignKeys.Single();
            var productIdFk = orderDetailType.ForeignKeys.Single(fk => fk.ReferencedEntityType == productType);
            var orderIdFk = orderDetailType.ForeignKeys.Single(fk => fk.ReferencedEntityType == orderType);

            productType.AddNavigation("OrderDetails", productIdFk, pointsToPrincipal: false);
            customerType.AddNavigation("Orders", customerIdFk, pointsToPrincipal: false);
            orderType.AddNavigation("Customer", customerIdFk, pointsToPrincipal: true);
            orderType.AddNavigation("OrderDetails", orderIdFk, pointsToPrincipal: false);
            orderDetailType.AddNavigation("Product", productIdFk, pointsToPrincipal: true);
            orderDetailType.AddNavigation("Order", orderIdFk, pointsToPrincipal: true);
        }

        public abstract NorthwindContext CreateContext();
    }
}
