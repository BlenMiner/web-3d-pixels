DELETE FROM Sessions
WHERE EndTime IS NOT NULL AND EndTime < $now;