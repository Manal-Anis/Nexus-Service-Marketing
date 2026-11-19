# Partner (vendor) registration and approval

| Step | Who | What happens |
|---|---|---|
| 1 | Visitor | Fills the partner form (business, owner, contact, city, details, password) |
| 2 | System | Saves Vendor = Pending and a locked login (IsActive = false, role Vendor, Vendor.UserId set) |
| 3 | Partner | Sign in is refused with "waiting for approval" |
| 4 | Admin | Approve: login unlocked, partner can sign in. Reject: stays locked, reason shown at sign in |
| 5 | Admin | Block: owner and all employees locked. Unblock: accounts open again |
| 6 | System | Vendor dashboard checks Vendor.Status = Approved on every request |

Accounts that have no login yet (created by the admin) receive one on approval with a temporary password
that is displayed once to the administrator.
