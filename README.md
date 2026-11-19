<div align="center">

# Nexus Service Marketing System

### A Complete Service Marketing and Management Web Application

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core%20MVC-.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-512BD4?style=for-the-badge)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)
![Chart.js](https://img.shields.io/badge/Chart.js-FF6384?style=for-the-badge&logo=chartdotjs&logoColor=white)

</div>

Nexus Service Marketing System is a web-based application designed to simplify and automate service marketing operations through a centralized digital platform.

The system provides an integrated environment where customers, vendors, and administrators can manage internet services, subscriptions, orders, connections, billing, payments, and business operations efficiently.

Developed as a group project using ASP.NET Core MVC (.NET 10), Microsoft SQL Server, Entity Framework Core, and ASP.NET Core Identity.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Key Features](#key-features)
- [Project Screenshots](#project-screenshots)
- [Technologies Used](#technologies-used)
- [Installation and Setup Guide](#installation-and-setup-guide)
- [Database Information](#database-information)
- [Demo Login Credentials](#demo-login-credentials)
- [Vendor Registration and Approval Process](#vendor-registration-and-approval-process)
- [Business Rules and Assumptions](#business-rules-and-assumptions)
- [Database SQL Script](#database-sql-script)
- [Running Tests](#running-tests)
- [Development Team](#development-team)
- [Project Objectives](#project-objectives)
- [Future Improvements](#future-improvements)
- [Acknowledgement](#acknowledgement)

---

## Project Overview

Nexus Service Marketing System aims to digitize the service marketing process by connecting customers with service providers through a centralized platform.

The application offers three main dashboards:

- **Customer Dashboard:** Allows customers to explore service plans, place orders, track connections, view bills, and manage their accounts.
- **Vendor Dashboard:** Enables registered partners to manage their business profiles, employees, service orders, and operations.
- **Admin Dashboard:** Provides centralized control over customers, vendors, employees, services, orders, connections, billing, inventory, and system settings.

The platform supports Dial-Up Internet, Broadband Internet, and Landline Connection services.

All customer payments are handled through Cash on Delivery (COD). Customers pay the installer after successful installation and activation.

---

## Key Features

### 1. Public Website

- Modern and responsive homepage.
- Service and subscription plan browsing.
- Dial-Up Internet, Broadband Internet, and Landline services.
- Customer registration and login.
- Vendor/partner registration.
- Connection availability checking.
- Online service order placement.
- Order tracking through Order ID.
- Retail partner information.
- Customer testimonials and feedback.
- Frequently Asked Questions (FAQs).
- About Us and Contact pages.
- Cash on Delivery payment information.

### 2. Customer Dashboard

- Customer registration and authentication.
- Customer profile management.
- Browse available service plans.
- Place new service orders.
- Track order status.
- View active connections.
- View bills and payment history.
- Upload identity and address documents (PDF, JPG, PNG up to 2 MB).
- Manage account information and change password.
- Submit feedback and contact support.

### 3. Vendor Dashboard

- Vendor registration and login.
- Vendor profile management.
- Dashboard with business information and charts.
- Service and order management.
- Feasibility survey queue.
- Connections, billing, and payments.
- Equipment stock and issued devices.
- Customer list and reports.
- Employee management.
- Access restricted until administrator approval.

### 4. Admin Panel

The administration panel provides complete management functionality.

**Dashboard and User Management**

- Admin Dashboard.
- Customer Management.
- Vendor Management.
- Employee Management.
- Retail Shops Management.
- City Management.

**Service Management**

- Service Plans Management.
- Dial-Up Internet Plans.
- Broadband Internet Plans.
- Landline Connection Plans.
- Service Settings.

**Inventory and Suppliers**

- Supplier Management.
- Products and Equipment Management.
- Purchase Orders.
- Supplier Payment Tracking.
- Reorder Suggestions.

**Orders and Financial Management**

- Orders Management.
- Connections Management.
- Bills Management.
- Payments Management.
- Customer and Order Search.

**System Management**

- Advanced Search.
- Feedback Management.
- Contact Messages Inbox.
- Audit Logs.
- Reports and Analytics.
- System Settings.
- Security and Access Control.

### 5. Authentication and Security

- ASP.NET Core Identity.
- Secure user authentication.
- Role-Based Authorization.
- Separate access for Customers, Vendors, and Administrators.
- Vendor approval and rejection system.
- Vendor account blocking and unblocking.
- Employee account management.
- Server-side authorization and access restrictions.

### 6. Payment System

- Cash on Delivery (COD).
- No online payment gateway.
- Customers pay installers after successful installation.
- Service tax calculation.
- Security deposit management.
- Bulk discount management.
- Billing and payment records.

---

## Project Screenshots

All screenshots are organized inside the `wwwroot/img/` directory.

---

### Public Website

**1. Home Page**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(71).png" alt="Home Page" width="100%">
</p>

**2. Service Overview and Featured Plans**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(72).png" alt="Service Overview" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(73).png" alt="Featured Service Plans" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Service Overview</sub></td>
    <td align="center"><sub>Featured Service Plans</sub></td>
  </tr>
</table>

**3. How It Works and Retail Partners**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(74).png" alt="How It Works" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(75).png" alt="Retail Partners" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>How It Works</sub></td>
    <td align="center"><sub>Retail Partners</sub></td>
  </tr>
</table>

**4. Testimonials and FAQs**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(76).png" alt="Customer Testimonials" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(77).png" alt="Frequently Asked Questions" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Customer Testimonials</sub></td>
    <td align="center"><sub>Frequently Asked Questions</sub></td>
  </tr>
</table>

**5. Get Connected and Footer**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(78).png" alt="Get Connected" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(79).png" alt="Website Footer" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Get Connected Section</sub></td>
    <td align="center"><sub>Website Footer</sub></td>
  </tr>
</table>

**6. Track Your Order and Check Your Connection**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(80).png" alt="Track Your Order" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(81).png" alt="Check Your Connection" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Track Your Order</sub></td>
    <td align="center"><sub>Check Your Connection by Account ID</sub></td>
  </tr>
</table>

**7. About Us**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(82).png" alt="About Us Hero" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(83).png" alt="Our History and Partner Workflow" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>About Us Hero Section</sub></td>
    <td align="center"><sub>Company History and How We Work With Partners</sub></td>
  </tr>
</table>

**8. Contact Page**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(84).png" alt="Contact Details" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(85).png" alt="Contact Form" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Contact Details</sub></td>
    <td align="center"><sub>Contact Form and Payment Notice</sub></td>
  </tr>
</table>

---

### Authentication and Registration

**9. Sign In and Demo Access**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(86).png" alt="Sign In" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(87).png" alt="Demo Access" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Sign In Page</sub></td>
    <td align="center"><sub>Demo Access Accounts</sub></td>
  </tr>
</table>

**10. Vendor (Partner) Registration**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(88).png" alt="Vendor Registration" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(89).png" alt="Vendor Registration Form" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Partner Registration (Top)</sub></td>
    <td align="center"><sub>Partner Registration (Form and Submit)</sub></td>
  </tr>
</table>

**11. Customer Registration and Change Password**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(90).png" alt="Customer Registration" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(101).png" alt="Change Password" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Customer Registration</sub></td>
    <td align="center"><sub>Change Password</sub></td>
  </tr>
</table>

---

### Customer Dashboard

**12. My Dashboard**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(124).png" alt="Customer Dashboard" width="100%">
</p>

**13. Browse Plans and My Orders**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(125).png" alt="Browse Plans" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(126).png" alt="My Orders" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Browse and Order Plans</sub></td>
    <td align="center"><sub>My Orders with Status Filters</sub></td>
  </tr>
</table>

**14. Connections, Bills and Payments**

<table>
  <tr>
    <td width="33%"><img src="wwwroot/img/Screenshot%20(127).png" alt="My Connections" width="100%"></td>
    <td width="33%"><img src="wwwroot/img/Screenshot%20(128).png" alt="My Bills" width="100%"></td>
    <td width="33%"><img src="wwwroot/img/Screenshot%20(129).png" alt="Payments and Receipts" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>My Connections</sub></td>
    <td align="center"><sub>My Bills</sub></td>
    <td align="center"><sub>Payments and Receipts</sub></td>
  </tr>
</table>

**15. Documents and Profile**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(130).png" alt="My Documents" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(131).png" alt="My Profile" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Document Upload</sub></td>
    <td align="center"><sub>Profile and Contact Details</sub></td>
  </tr>
</table>

**16. Customer Feedback**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(132).png" alt="Customer Feedback" width="70%">
</p>

---

### Vendor Dashboard

**17. Partner Dashboard**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(97).png" alt="Partner Dashboard" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(92).png" alt="Dashboard Orders and Connections" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Partner Dashboard Overview</sub></td>
    <td align="center"><sub>Orders Waiting for Survey and Latest Connections</sub></td>
  </tr>
</table>

**18. Orders and Advanced Search**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(94).png" alt="Orders" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(93).png" alt="Advanced Search" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Orders with Status Filters</sub></td>
    <td align="center"><sub>Advanced Search and CSV Export</sub></td>
  </tr>
</table>

**19. Feasibility Queue and Connections**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(95).png" alt="Feasibility Queue" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(96).png" alt="Connections" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Feasibility Queue</sub></td>
    <td align="center"><sub>Live Connections</sub></td>
  </tr>
</table>

**20. Equipment and Customers**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(98).png" alt="Equipment" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(99).png" alt="Customers" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Equipment Stock and Issued Devices</sub></td>
    <td align="center"><sub>Customer List</sub></td>
  </tr>
</table>

**21. Billing and Payments**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(102).png" alt="Vendor Billing" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(103).png" alt="Vendor Payments" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Billing and Invoices</sub></td>
    <td align="center"><sub>Cash Payments and Security Deposit Ledger</sub></td>
  </tr>
</table>

**22. Vendor Reports**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(100).png" alt="Vendor Reports" width="85%">
</p>

---

### Admin Panel

**23. Administrator Dashboard**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(104).png" alt="Administrator Dashboard" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(105).png" alt="Dashboard Charts" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Network Overview and Key Numbers</sub></td>
    <td align="center"><sub>Connections, Orders per Month and Busiest Cities</sub></td>
  </tr>
</table>

**24. Vendors and Employees**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(106).png" alt="Service Partners" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(107).png" alt="Employees" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Service Partners Approval</sub></td>
    <td align="center"><sub>Employee Management</sub></td>
  </tr>
</table>

**25. Retail Shops and Customers**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(108).png" alt="Retail Shops" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(109).png" alt="Customers" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Retail Shops</sub></td>
    <td align="center"><sub>Customer Management with CSV Export</sub></td>
  </tr>
</table>

**26. Plans Management**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(110).png" alt="Plans Management" width="80%">
</p>

**27. Equipment and Suppliers**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(111).png" alt="Equipment" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(112).png" alt="Suppliers" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Equipment and Stock Control</sub></td>
    <td align="center"><sub>Supplier Management</sub></td>
  </tr>
</table>

**28. Purchase Orders**

<p align="center">
  <img src="wwwroot/img/Screenshot%20(113).png" alt="Purchase Orders" width="75%">
</p>

**29. Orders and Connections**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(114).png" alt="Admin Orders" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(115).png" alt="Admin Connections" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Network Orders</sub></td>
    <td align="center"><sub>Network Connections</sub></td>
  </tr>
</table>

**30. Bills and Payments**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(116).png" alt="Admin Bills" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(117).png" alt="Admin Payments" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Invoices and Periodic Bills</sub></td>
    <td align="center"><sub>Cash Register and Receipts</sub></td>
  </tr>
</table>

**31. Cities and System Settings**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(118).png" alt="Cities" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(119).png" alt="Settings" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>City Codes</sub></td>
    <td align="center"><sub>Tax, Deposits and Bulk Discount Tiers</sub></td>
  </tr>
</table>

**32. Feedback and Contact Messages**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(120).png" alt="Feedback Management" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(121).png" alt="Contact Messages" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Feedback Review and Publishing</sub></td>
    <td align="center"><sub>Contact Messages Inbox</sub></td>
  </tr>
</table>

**33. Audit Log and Reports**

<table>
  <tr>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(122).png" alt="Audit Log" width="100%"></td>
    <td width="50%"><img src="wwwroot/img/Screenshot%20(123).png" alt="Admin Reports" width="100%"></td>
  </tr>
  <tr>
    <td align="center"><sub>Audit Log</sub></td>
    <td align="center"><sub>Reports and Analytics</sub></td>
  </tr>
</table>

---

## Technologies Used

| Category          | Technologies               |
| ----------------- | -------------------------- |
| Frontend          | HTML5, CSS3, JavaScript    |
| Backend           | C#                         |
| Framework         | ASP.NET Core MVC (.NET 10) |
| Database          | Microsoft SQL Server       |
| ORM               | Entity Framework Core      |
| Authentication    | ASP.NET Core Identity      |
| Architecture      | MVC Architecture           |
| Database Approach | Code First                 |
| UI Framework      | Bootstrap 5                |
| Charts            | Chart.js                   |
| IDE               | Visual Studio              |
| Version Control   | Git and GitHub             |

---

## Installation and Setup Guide

Follow these steps to download, configure, and run the project on your computer.

### 1. Prerequisites

Make sure the following software is installed:

- Visual Studio
- .NET 10 SDK
- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Git

### 2. Clone the Repository

Open PowerShell or the Visual Studio terminal.

```
git clone YOUR_GITHUB_REPOSITORY_LINK
```

Navigate to the project directory:

```
cd NexusServiceMarketingSystem
```

Open the solution file in Visual Studio.

### 3. Configure SQL Server Database

This project uses Microsoft SQL Server with Entity Framework Core Code First.

1. Open SQL Server Management Studio.
2. Connect to your installed SQL Server instance.
3. Use the following database name:

```
NexusServiceMarketingDb
```

**Important:** Configure your actual SQL Server instance. Do not use LocalDB.

### 4. Configure Connection String

Open:

```
appsettings.json
```

Locate the `ConnectionStrings` section and update `DefaultConnection`.

Example:

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=NexusServiceMarketingDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

Replace `YOUR_SERVER_NAME` with your actual SQL Server instance name.

For example:

```
Server=YOUR_SERVER_NAME\\YOUR_INSTANCE
```

Make sure the database name remains:

```
NexusServiceMarketingDb
```

### 5. Restore NuGet Packages

Open Package Manager Console:

Tools → NuGet Package Manager → Package Manager Console

Run:

```
Update-Package -reinstall
```

Alternatively, use:

```
dotnet restore
```

### 6. Apply Database Migrations

The project already contains an initial Entity Framework Core migration.

Open Package Manager Console and execute:

```
Update-Database
```

This creates the database structure and applies the existing migrations.

Do not create another initial migration if the migration files are already included in the repository.

### 7. Run the Application

1. Open the solution in Visual Studio.
2. Set the MVC project as the Startup Project.
3. Make sure SQL Server is running.
4. Build the solution.
5. Press F5 or click the Run button.

Alternatively, run:

```
dotnet run
```

Open the application using the URL displayed in the terminal.

The application also applies its database migrations and initializes demo data during its first startup, as configured in the project.

---

## Database Information

| Property              | Details                          |
| --------------------- | -------------------------------- |
| Database Name         | NexusServiceMarketingDb          |
| Database Server       | Microsoft SQL Server             |
| ORM                   | Entity Framework Core            |
| Database Approach     | Code First                       |
| DbContext             | ApplicationDbContext             |
| Namespace             | NexusServiceMarketingSystem.Data |
| Connection String Key | DefaultConnection                |

---

## Demo Login Credentials

The following accounts are available for testing the application.

| Role            | Email                                               | Password     |
| --------------- | --------------------------------------------------- | ------------ |
| Administrator   | [admin@nexus.com](mailto:admin@nexus.com)           | Admin@123    |
| Vendor Owner    | [vendor1@nexus.com](mailto:vendor1@nexus.com)       | Vendor@123   |
| Technical Staff | [technical1@nexus.com](mailto:technical1@nexus.com) | Staff@123    |
| Accounts Staff  | [accounts1@nexus.com](mailto:accounts1@nexus.com)   | Staff@123    |
| Retail Staff    | [retail1@nexus.com](mailto:retail1@nexus.com)       | Staff@123    |
| Customer        | [customer1@nexus.com](mailto:customer1@nexus.com)   | Customer@123 |

The demo credentials are also displayed on the Login page and Admin Portal page.

To hide demo credentials, configure the following in `appsettings.json`:

```
"Demo": {
  "ShowCredentials": false
}
```

### Where to Sign In

**Customers and Partners:**

```
/Account/Login
```

**Administrators:**

```
/Account/AdminLogin
```

The Admin Portal link is also available in the website footer.

Opening any `/Admin/...` page without authentication redirects the user to the Admin Login page.

---

## Vendor Registration and Approval Process

The application includes a complete vendor registration and approval workflow.

1. A visitor clicks the **Join as Partner** button.
2. The visitor completes the vendor registration form, including a password.
3. The system saves the vendor request with Pending status.
4. A locked login account is created for the vendor.
5. Before approval, the vendor cannot access the dashboard and receives an approval-pending message.
6. The administrator opens Admin → Vendors.
7. The administrator reviews the application and selects Approve or Reject.
8. Once approved, the vendor account is unlocked.
9. The vendor can log in using the email and password provided during registration.
10. The vendor is redirected to the Vendor Dashboard.

### Additional Vendor Management

- Block: Locks the vendor owner and all associated employees.
- Unblock: Restores their access.
- Server-side restrictions prevent unapproved vendors from accessing the Vendor Dashboard.
- Vendors created directly by the administrator can receive a temporary password during approval.
- Temporary passwords are displayed once to the administrator.
- Employees (Technical, Accounts, and Retail) are created by the administrator through Admin → Employees.

---

## Business Rules and Assumptions

The application follows these business rules:

| Feature              | Business Rule                            |
| -------------------- | ---------------------------------------- |
| Order ID             | 1 letter + 10 digits                     |
| Account ID           | 1 letter + 3-digit city code + 12 digits |
| Payment Method       | Cash on Delivery                         |
| Service Tax          | 12.24% on the whole bill                 |
| Currency             | Pakistani Rupees (Rs.)                   |
| Landline Yearly Plan | Rs. 7,500                                |

### Bulk Discount Structure

| Order Quantity | Discount |
| -------------- | -------- |
| 10–15          | 25%      |
| 16–25          | 50%      |
| 26–50          | 75%      |
| 51+            | 100%     |

The landline yearly plan price is an assumed value because the original specification did not provide one.

---

## Database SQL Script

If a standalone SQL database script is required, the project includes:

```
database/generate-sql.cmd
```

Run this file to generate the SQL script from the existing Entity Framework Core migrations using:

```
dotnet ef migrations script
```

The generated SQL file can be used when a database script is required for project submission.

---

## Running Tests

The project includes a test project.

To execute the tests, run:

```
dotnet test tests/NexusServiceMarketingSystem.Tests
```

---

## Development Team

This project was collaboratively developed by:

| Team Member      | Role      |
| ---------------- | --------- |
| Manal Anis       | Developer |
| Alishba Iftikhar | Developer |

**Project Type:** Group Project

---

## Project Objectives

- Digitize service marketing operations.
- Simplify customer and vendor management.
- Provide centralized service and subscription management.
- Improve order and connection tracking.
- Organize billing and payment operations.
- Manage vendors, employees, suppliers, and inventory.
- Provide efficient administrative control.
- Implement secure role-based authentication.
- Deliver a user-friendly web-based platform.

---

## Future Improvements

- Enhanced reporting and analytics.
- Advanced notification system.
- Improved customer analytics.
- Additional payment gateway integrations.
- Cloud deployment and hosting.
- Improved mobile responsiveness.

---

## Acknowledgement

Nexus Service Marketing System was developed collaboratively with dedication, teamwork, and a focus on practical software development.

The project demonstrates the implementation of a complete service marketing and management platform using modern web technologies, database management, and role-based authentication.

---

<div align="center">

**Nexus Service Marketing System**

*Connecting services, simplifying management, and empowering businesses.*

</div>
