## Background  
Heroku's free-tier is pricing is confusing and can get expensive. However, if you have a set of website that barely receive any traffic, combination of Hetzner and Coolify is great.

## How to:

```text
Domain: hesamian.com

Name: coolify
Type: A
Value: <Hetzner Coolify server>

Name: *.coolify
Type: CNAME
Value: coolify.hesamian.com
```
