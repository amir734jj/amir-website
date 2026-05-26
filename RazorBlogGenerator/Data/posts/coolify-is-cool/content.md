## Background  
Heroku's free-tier is pricing is confusing and can get expensive. However, if you have a set of website that barely receive any traffic, combination of Hetzner and Coolify is great.

## How to:
Coolify already handle the domain + reverse-proxy + ssl. Just make sure domains have this comma separated format

```
https://<app>.coolify.hesamian.com,https://www.<app>.coolify.hesamian.com
```

And to setup the domain:
```
Domain: hesamian.com

Name: coolify
Type: A
Value: <Hetzner Coolify server>

Name: *.coolify
Type: CNAME
Value: coolify.hesamian.com
```
