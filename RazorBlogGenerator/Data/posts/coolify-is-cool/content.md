## Background  
Heroku's free-tier pricing is confusing and can be expensive. However, if you have a set of websites that barely receive any traffic, a combination of Hetzner and Coolify is great.

## How to:
Coolify already handles the domain + reverse-proxy + ssl. Just make sure domains have this comma-separated format

```
https://<app>.coolify.hesamian.com,https://www.<app>.coolify.hesamian.com
```

And to set up the domain:
```
Domain: hesamian.com

Name: coolify
Type: A
Value: <Hetzner Coolify server IPv4>

Name: coolify
Type: AAAA
Value: <Hetzner Coolify server IPv6>

Name: *.coolify
Type: CNAME
Value: coolify.hesamian.com
```
