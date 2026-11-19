# Nexus Service Marketing System

ASP.NET Core MVC (.NET 10) + SQL Server + Entity Framework Core + ASP.NET Core Identity.
Three dashboards: **Customer**, **Vendor (partner)** and **Admin**. Payment is Cash on Delivery only.

## Run it
1. Install the **.NET 10 SDK** and **SQL Server** (LocalDB or Express).
2. Open `appsettings.json` and set `ConnectionStrings:DefaultConnection` for your own computer.
   The file contains the connection string of the original developer PC (`MANALLAPTOP`), change it, for example:
   `Server=(localdb)\\MSSQLLocalDB;Database=NexusServiceMarketingDb;Trusted_Connection=True;TrustServerCertificate=True`
3. In this folder run `dotnet run` and open the address that is printed (see `Properties/launchSettings.json`).
4. The first start creates the database, applies the migration and inserts the demo data.

## Demo logins (also shown on the Login page and on the Admin portal page)
| Role | Email | Password |
|---|---|---|
| Administrator | admin@nexus.com | Admin@123 |
| Partner (vendor owner) | vendor1@nexus.com | Vendor@123 |
| Technical / Accounts / Retail staff | technical1@ / accounts1@ / retail1@nexus.com | Staff@123 |
| Customer | customer1@nexus.com | Customer@123 |

The demo panel can be switched off with `"Demo": { "ShowCredentials": false }` in `appsettings.json`.

## Where to sign in
* Customers and partners: `/Account/Login`
* Administrators: `/Account/AdminLogin` (link "Admin portal" in the footer). Opening any `/Admin/...` address
  without a login also leads there.

## How a partner (vendor) joins - complete flow
1. The visitor clicks **Join as partner** (navigation bar, home page, login page, customer registration page or footer).
2. He fills the registration form **including a password**. The system stores the request as *Pending* and creates a
   **locked** login account (cannot sign in yet).
3. Trying to sign in before the approval shows: "waiting for the approval of the administrator".
4. The administrator opens **Admin > Vendors**, checks the request and clicks **Approve** (or **Reject**).
5. Approval unlocks the partner and his employees. The partner signs in with the email and password of step 2 and lands
   on the Vendor dashboard.
6. **Block** locks the owner and every employee at once, **Unblock** opens them again. The vendor dashboard is also closed
   server side for every partner that is not approved.
7. A partner created by the administrator (Admin > Vendors > Register a partner) or an old request without a login gets a
   login with a **temporary password on approval**; the password is shown once on the screen for the administrator.
8. Employees (Technical, Accounts, Retail) are created by the administrator under Admin > Employees.

## Database script
`database/generate-sql.cmd` creates the full SQL script from the migration (`dotnet ef migrations script`).
Run it once if the examiner needs a `.sql` file.

## Tests
`dotnet test tests/NexusServiceMarketingSystem.Tests`

## Assumptions
* Order ID = 1 letter + 10 digits; Account ID = 1 letter + 3 digit city code + 12 digits.
* Bulk discount ranges: 10-15 = 25%, 16-25 = 50%, 26-50 = 75%, 51+ = 100%.
* Landline yearly plan price (blank in the specification) is assumed as Rs 7,500.
* Service tax 12.24% on the whole bill. Currency: Rs. No online payment.
