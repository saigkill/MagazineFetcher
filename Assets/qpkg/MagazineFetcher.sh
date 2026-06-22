#!/bin/sh

# ---------------------------------------------------------
# MagazineFetcher – QNAP Start/Stop Script
# ---------------------------------------------------------
# Dieses Script wird von QNAP über QPKG_SERVICE_PROGRAM
# aufgerufen. Es startet und stoppt den .NET-Dienst.
# ---------------------------------------------------------

QPKG_NAME="MagazineFetcher"
QPKG_DIR="$(dirname "$0")"
APP_DIR="${QPKG_DIR}/build"
LOG_DIR="${QPKG_DIR}/logs"
PID_FILE="${QPKG_DIR}/magazinefetcher.pid"

DOTNET_BIN="/usr/local/dotnet/dotnet"   # Falls du dotnet mitlieferst, anpassen
APP_DLL="${APP_DIR}/MagazineFetcher.dll"

mkdir -p "$LOG_DIR"

start_app() {
    if [ -f "$PID_FILE" ] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
        echo "$QPKG_NAME already running."
        exit 0
    fi

    echo "Launching $QPKG_NAME..."

    nohup $DOTNET_BIN "$APP_DLL" \
        >> "$LOG_DIR/output.log" 2>&1 &

    echo $! > "$PID_FILE"

    echo "$QPKG_NAME started with (PID $(cat "$PID_FILE"))."
}

stop_app() {
    if [ ! -f "$PID_FILE" ]; then
        echo "$QPKG_NAME doesn't running."
        exit 0
    fi

    PID=$(cat "$PID_FILE")

    echo "Stopping $QPKG_NAME (PID $PID)..."

    kill "$PID" 2>/dev/null

    # Warten bis Prozess beendet ist
    for i in $(seq 1 10); do
        if kill -0 "$PID" 2>/dev/null; then
            sleep 1
        else
            break
        fi
    done

    if kill -0 "$PID" 2>/dev/null; then
        echo "Prozess doesn't shows any reaction – forcing Kill."
        kill -9 "$PID" 2>/dev/null
    fi

    rm -f "$PID_FILE"
    echo "$QPKG_NAME stopped."
}

case "$1" in
    start)
        start_app
        ;;
    stop)
        stop_app
        ;;
    restart)
        stop_app
        start_app
        ;;
    *)
        echo "Usage: $0 {start|stop|restart}"
        exit 1
        ;;
esac

exit 0
