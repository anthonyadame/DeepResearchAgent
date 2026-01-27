# Production Deployment Checklist - APO Monitoring Stack

## Pre-Deployment

### Infrastructure
- [ ] Docker host meets minimum requirements (4GB RAM, 20GB disk)
- [ ] Persistent volumes configured for Prometheus data
- [ ] Persistent volumes configured for Grafana data
- [ ] Network firewall rules allow Prometheus scraping
- [ ] SSL/TLS certificates obtained for Grafana (if HTTPS required)
- [ ] Backup storage configured and tested

### Configuration Review
- [ ] Reviewed `prometheus.yml` for correct scrape targets
- [ ] Reviewed `apo-alerts.yml` for appropriate thresholds
- [ ] Reviewed `alertmanager.yml` for notification channels
- [ ] Reviewed `docker-compose.yml` resource limits
- [ ] Timezone configured correctly in all services

## Security

### Grafana
- [ ] Changed default admin password from `admin`
- [ ] Disabled anonymous access (`GF_AUTH_ANONYMOUS_ENABLED=false`)
- [ ] Configured OAuth/LDAP/SAML if required
- [ ] Enabled HTTPS with valid certificate
- [ ] Configured `GF_SERVER_ROOT_URL` to public URL
- [ ] Set strong `GF_SECURITY_SECRET_KEY`
- [ ] Disabled sign-up (`GF_USERS_ALLOW_SIGN_UP=false`)
- [ ] Configured RBAC roles and permissions

### Prometheus
- [ ] Enabled basic auth or OAuth proxy
- [ ] Restricted network access to trusted IPs
- [ ] Configured read-only remote storage if applicable
- [ ] Reviewed metric retention period (default: 30 days)
- [ ] Disabled admin API if not needed

### Alertmanager
- [ ] Enabled basic auth or proxy authentication
- [ ] Secured webhook endpoints
- [ ] Validated SMTP credentials are not hardcoded
- [ ] Used secrets management for sensitive data

### Docker
- [ ] Removed `--privileged` flags
- [ ] Set resource limits (CPU, memory)
- [ ] Configured container restart policies
- [ ] Used non-root user where possible
- [ ] Scanned images for vulnerabilities

## Notifications

### Email
- [ ] SMTP server configured and tested
- [ ] Email templates customized
- [ ] Distribution lists created
- [ ] Test email sent successfully
- [ ] SPF/DKIM configured for domain

### Slack
- [ ] Webhook URL configured
- [ ] Channels created (#critical-alerts, #warnings, #info)
- [ ] Bot permissions verified
- [ ] Test alert sent and received
- [ ] Channel notification preferences set

### Microsoft Teams
- [ ] Incoming webhook created
- [ ] Channel configured
- [ ] Test notification sent
- [ ] Permissions verified

### PagerDuty (Optional)
- [ ] Service integration key configured
- [ ] Escalation policy defined
- [ ] Oncall schedule created
- [ ] Integration tested

## Monitoring Configuration

### Scrape Targets
- [ ] Deep Research Agent endpoint reachable
- [ ] Lightning Server endpoint reachable (if applicable)
- [ ] Targets showing as "UP" in Prometheus UI
- [ ] Metrics flowing to Prometheus (check Targets page)

### Alert Rules
- [ ] All 16 alert rules loaded successfully
- [ ] Alert thresholds reviewed and adjusted for environment
- [ ] Alert `for` durations appropriate for use case
- [ ] Test alerts fired and routed correctly
- [ ] Inhibition rules tested

### Dashboards
- [ ] APO Performance dashboard imported
- [ ] All panels showing data
- [ ] Datasource connected successfully
- [ ] Variables configured (if any)
- [ ] Permissions set appropriately

## Data Retention & Storage

### Prometheus
- [ ] Retention period configured (`--storage.tsdb.retention.time`)
- [ ] Disk space monitored (alert at 80% full)
- [ ] Storage path has appropriate permissions
- [ ] Backup strategy defined and tested
- [ ] Remote write configured (if using Thanos/Cortex)

### Grafana
- [ ] Database backup configured
- [ ] Dashboard export automated
- [ ] User data backup included
- [ ] Disaster recovery tested

## Performance & Scaling

### Resource Allocation
- [ ] CPU limits appropriate for load
- [ ] Memory limits prevent OOM kills
- [ ] Disk I/O performance adequate
- [ ] Network bandwidth sufficient

### Prometheus
- [ ] Scrape interval appropriate (default: 15s)
- [ ] Evaluation interval appropriate (default: 15s)
- [ ] Query timeout configured
- [ ] Sample limit configured if needed
- [ ] Federation configured for multi-cluster (if needed)

### Grafana
- [ ] Max concurrent queries configured
- [ ] Query timeout set
- [ ] Image rendering configured (if using alerts)
- [ ] Caching configured for frequently used queries

## High Availability (Optional)

- [ ] Multiple Prometheus instances configured
- [ ] Prometheus federation or Thanos deployed
- [ ] Alertmanager clustering configured
- [ ] Grafana load balancer configured
- [ ] Shared storage for dashboards
- [ ] Database replication for Grafana (if using external DB)

## Documentation

- [ ] Internal runbook created
- [ ] Oncall playbook documented
- [ ] Escalation procedures defined
- [ ] Known issues documented
- [ ] Architecture diagram updated
- [ ] Contact information current

## Testing

### Functional Testing
- [ ] Metrics flowing to Prometheus
- [ ] Dashboards displaying correctly
- [ ] Alerts firing as expected
- [ ] Notifications received on all channels
- [ ] Queries returning expected results

### Load Testing
- [ ] Tested with realistic metric volume
- [ ] Verified performance under load
- [ ] Confirmed retention policy works
- [ ] Validated backup/restore procedures

### Failure Testing
- [ ] Tested Prometheus restart (data persists)
- [ ] Tested Grafana restart (config persists)
- [ ] Tested network partition scenarios
- [ ] Tested disk full scenario
- [ ] Tested alert routing with service failures

## Monitoring the Monitoring

### Prometheus Self-Monitoring
- [ ] Prometheus metrics enabled
- [ ] Alerts for Prometheus scrape failures
- [ ] Alerts for Prometheus disk space
- [ ] Alerts for Prometheus down

### Grafana Health
- [ ] Grafana health check endpoint monitored
- [ ] Alerts for Grafana down
- [ ] Dashboard rendering performance tracked

### Alertmanager Health
- [ ] Alertmanager clustering status monitored
- [ ] Notification delivery tracked
- [ ] Alerts for Alertmanager down

## Compliance & Audit

- [ ] Logging enabled for all services
- [ ] Audit logs retained per policy
- [ ] Access logs reviewed regularly
- [ ] Compliance requirements met (GDPR, HIPAA, etc.)
- [ ] Data retention policy documented

## Go-Live

### Pre-Launch
- [ ] All checklist items completed
- [ ] Stakeholders notified
- [ ] Maintenance window scheduled (if needed)
- [ ] Rollback plan prepared
- [ ] Team trained on new monitoring

### Launch
- [ ] Services started in correct order
- [ ] Health checks passing
- [ ] Metrics flowing
- [ ] Dashboards accessible
- [ ] Alerts configured

### Post-Launch
- [ ] Monitor for 24 hours
- [ ] Review alert volume (adjust if too noisy)
- [ ] Collect team feedback
- [ ] Document lessons learned
- [ ] Schedule review meeting

## Maintenance

### Daily
- [ ] Check alert volume (should be low)
- [ ] Review critical alerts
- [ ] Verify all targets "UP"

### Weekly
- [ ] Review dashboard performance
- [ ] Check disk space utilization
- [ ] Review alert effectiveness
- [ ] Analyze false positive rate

### Monthly
- [ ] Review and update alert thresholds
- [ ] Optimize slow queries
- [ ] Clean up unused dashboards
- [ ] Update documentation
- [ ] Test backup restore

### Quarterly
- [ ] Review retention policies
- [ ] Update to latest stable versions
- [ ] Security vulnerability scan
- [ ] Capacity planning review
- [ ] Disaster recovery drill

## Emergency Contacts

| Role | Name | Contact | Backup |
|------|------|---------|--------|
| Primary Oncall | _______ | _______ | _______ |
| Secondary Oncall | _______ | _______ | _______ |
| Manager | _______ | _______ | _______ |
| Infrastructure | _______ | _______ | _______ |

## Rollback Plan

1. Stop monitoring stack: `docker-compose down`
2. Restore previous configuration from Git
3. Restore Prometheus data from backup
4. Restore Grafana database from backup
5. Restart with previous version
6. Verify services healthy
7. Notify stakeholders

## Success Criteria

- [ ] Zero unplanned downtime in first week
- [ ] < 5 false positive alerts per day
- [ ] All critical alerts actionable
- [ ] Dashboard load time < 2 seconds
- [ ] 95% team adoption within 2 weeks
- [ ] Positive team feedback
- [ ] Zero data loss incidents

## Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| DevOps Lead | _______ | _______ | _______ |
| SRE Lead | _______ | _______ | _______ |
| Security | _______ | _______ | _______ |
| Manager | _______ | _______ | _______ |

---

**Deployment Date:** _________________  
**Version:** 1.0  
**Next Review:** _________________

