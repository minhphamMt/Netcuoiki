(function () {
    const connectionStatus = document.getElementById('connectionStatus');
    const feed = document.getElementById('notificationFeed');
    const input = document.getElementById('notificationInput');
    const form = document.getElementById('notificationForm');
    const currentUserElement = document.getElementById('currentUser');

    if (!feed || !form) {
        return;
    }

    const user = currentUserElement ? currentUserElement.value : 'Ẩn danh';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/notificationHub')
        .withAutomaticReconnect()
        .build();

    function appendMessage(author, message, timestamp) {
        const container = document.createElement('div');
        container.className = 'mb-2';

        const header = document.createElement('div');
        header.className = 'small text-muted';
        header.textContent = `${author} • ${timestamp}`;

        const body = document.createElement('div');
        body.className = 'fw-semibold';
        body.textContent = message;

        container.appendChild(header);
        container.appendChild(body);
        feed.appendChild(container);
        feed.scrollTop = feed.scrollHeight;
    }

    connection.on('ReceiveNotification', function (author, message, timestamp) {
        appendMessage(author, message, timestamp);
    });

    connection.start().then(function () {
        if (connectionStatus) {
            connectionStatus.textContent = 'Đã kết nối';
            connectionStatus.classList.remove('bg-light', 'text-dark');
            connectionStatus.classList.add('bg-success', 'text-white');
        }
    }).catch(function () {
        if (connectionStatus) {
            connectionStatus.textContent = 'Lỗi kết nối';
            connectionStatus.classList.remove('bg-light', 'text-dark');
            connectionStatus.classList.add('bg-danger', 'text-white');
        }
    });

    form.addEventListener('submit', function (event) {
        event.preventDefault();
        const message = input.value.trim();
        if (!message) {
            return;
        }

        connection.invoke('SendNotification', user, message)
            .catch(function () {
                if (connectionStatus) {
                    connectionStatus.textContent = 'Không gửi được';
                    connectionStatus.classList.remove('bg-success');
                    connectionStatus.classList.add('bg-warning', 'text-dark');
                }
            });

        input.value = '';
        input.focus();
    });
})();