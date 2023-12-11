from datetime import datetime

def get_current_date_time():
    return datetime.utcnow().strftime('%Y-%m-%dT%H:%M:%SZ')