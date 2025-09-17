$(document).ready(function () {

    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'))
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl)
    })
    let editor;
    let currentFileAddress = '';
    let currentXmlContent = '';
    let hoverTimeout;
    let tooltipTarget = $('#editor-tooltip-target');

   
    tooltipTarget.popover({
        trigger: 'manual',
        placement: 'right',
        html: true,
        container: 'body'
    });

    // --- HELPER FUNCTIONS ---
    function initializeEditor() {
        if (editor) {
            return; 
        }

        editor = CodeMirror.fromTextArea(document.getElementById('viewer-content'), {
            lineNumbers: true,
            mode: "xml",
            theme: "material-darker",
            readOnly: false
        });

        editor.on("cursorActivity", function () {
            const cursor = editor.getCursor();
            let statusText = `Ln ${cursor.line + 1}, Col ${cursor.ch + 1}`;
            const token = editor.getTokenAt(cursor);

            if (token && token.type === 'tag' && token.string.trim().length > 1) {
                let pathParts = [];
                let currentContext = token.state.context;
                while (currentContext) {
                    if (currentContext.tagName) {
                        pathParts.unshift(currentContext.tagName);
                    }
                    currentContext = currentContext.prev;
                }
                const path = `/${pathParts.join('/')}`;
                statusText += ` — Path: ${path}`;
            }
            $('#tag-details').text(statusText);
        });

        editor.on("dblclick", function () {
            if (currentXmlContent) {
                const selectedText = editor.getSelection().trim();
                if (selectedText) {
                    $.post('/Explorer/GetXPathFromSelection', { xmlContent: currentXmlContent, selectedText: selectedText }, function (xpath) {
                        navigator.clipboard.writeText(xpath).then(() => {
                            alert(`XPath copied to clipboard:\n\n${xpath}`);
                        });
                    }).fail(function (err) {
                        alert("Error getting XPath: " + err.responseText);
                    });
                }
            }
        });

        const editorWrapper = editor.getWrapperElement();
        const tooltipTarget = $('#editor-tooltip-target');

        $(editorWrapper).on('mousemove', function (e) {
            clearTimeout(hoverTimeout);

            hoverTimeout = setTimeout(function () {
                const pos = editor.coordsChar({ left: e.clientX, top: e.clientY });
                const token = editor.getTokenAt(pos);

                tooltipTarget.popover('dispose');

                if (token && token.type === 'tag' && token.string.trim().length > 1) {
                    let pathParts = [];
                    let currentContext = token.state.context;
                    while (currentContext) {
                        if (currentContext.tagName) { pathParts.unshift(currentContext.tagName); }
                        currentContext = currentContext.prev;
                    }
                    const path = `/${pathParts.join('/')}/${token.string}`;
                    const lineNumber = `Ln: ${pos.line + 1}`;
                    const popoverContent = `<div style="font-family: monospace; font-size: 0.85rem;"><div><strong>Path:</strong> ${path}</div><div><strong>${lineNumber}</strong></div></div>`;

                    tooltipTarget
                        .css({ top: e.clientY + 5, left: e.clientX + 5 })
                        .popover({
                            content: popoverContent,
                            trigger: 'manual',
                            placement: 'right',
                            html: true,
                            container: 'body'
                        })
                        .popover('show');
                }
            }, 150); 
        });

        // The mouseleave event just needs to destroy any active popover
        $(editorWrapper).on('mouseleave', function () {
            clearTimeout(hoverTimeout);
            tooltipTarget.popover('dispose');
        });
    }

    function setActiveLink(element) {
        $('.sidebar .nav-link, .sidebar .connection-card').removeClass('active');
        $(element).addClass('active');
    }

    function fetchRemoteFiles(provider, host, username, password, remoteDir) {
        $('#file-list-container').html('<div class="text-center p-5">Connecting...</div>');
        $('#file-viewer-container').hide();
        $('#file-list-container').show();
        let url = provider === 'FTP' ? '/Explorer/GetFtpFiles' : '/Explorer/GetSftpFiles';
        $.post(url, { host, username, password, remoteDir }, function (data) {
            $('#file-list-container').html(data);
        }).fail(function (err) {
            $('#file-list-container').html(`<div class="alert alert-danger">Error: ${err.responseText}</div>`);
        });
    }

    // Handles clicking on a local drive or a virtual file in the sidebar.
    $('.local-path').on('click', function (e) {
        e.preventDefault();
        setActiveLink(this);
        const path = $(this).data('path');
        $('#file-list-container').html('<div class="text-center p-5">Loading...</div>');
        $('#file-viewer-container').hide();
        $('#file-list-container').show();
        $('#file-list-container').load(`/Explorer/GetLocalFiles?path=${encodeURIComponent(path)}`, function (response, status, xhr) {
            if (status == "error") {
                $('#file-list-container').html(`<div class="alert alert-danger">${xhr.responseText}</div>`);
            }
        });
    });

    // Handles clicking on a remote connection card (FTP/SFTP).
    $('.remote-path').on('click', function (e) {
        e.preventDefault();
        setActiveLink(this);
        const link = $(this);
        const provider = link.data('provider');
        const host = link.data('host');
        const username = link.data('username');
        const password = link.data('password');
        const remoteDir = link.data('remotedir');

        if (username && password) {
            fetchRemoteFiles(provider, host, username, password, remoteDir);
        } else {
            $('#modal-provider').val(provider);
            $('#modal-host').val(host);
            $('#modal-remoteDir').val(remoteDir);
            $('#credentialsModal').modal('show');
        }
    });

    // Handles submitting credentials from the FTP/SFTP modal.
    $('#submitCredentials').on('click', function () {
        const provider = $('#modal-provider').val();
        const host = $('#modal-host').val();
        const remoteDir = $('#modal-remoteDir').val();
        const username = $('#modal-username').val();
        const password = $('#modal-password').val();

        fetchRemoteFiles(provider, host, username, password, remoteDir);

        $('#credentialsModal').modal('hide');
        $('#credentialsForm')[0].reset();
    });



    // Handles clicking the "View" button for a specific file in the list.
    $(document).on('click', '.view-file-btn', function () {
        const btn = $(this);
        const provider = btn.data('provider');
        const fullPath = btn.data('fullpath');
        const host = btn.data('host');
        const username = btn.data('username');
        const password = btn.data('password');
        currentFileAddress = fullPath;

        const fileName = fullPath.split(/[/\\]/).pop();
        const fileExtension = fileName.split('.').pop().toUpperCase() || 'File';

        const sizeInBytes = parseInt(btn.data('size'), 10);
        let fileSizeFormatted = "0 KB";

        if (!isNaN(sizeInBytes)) {
            fileSizeFormatted = (sizeInBytes / 1024).toFixed(2) + " KB";
        }

        $('#open-file-name').text(fileName);
        $('#open-file-type').text(fileExtension);
        $('#open-file-size').text(fileSizeFormatted); 
        $('#open-file-path').text(fullPath);
        $('#open-file-details').show();

        const isTextFile = fullPath.toLowerCase().endsWith('.xml') || fullPath.toLowerCase().endsWith('.json') || fullPath.toLowerCase().endsWith('.txt');

        $.ajax({
            url: '/Explorer/ViewFile',
            type: 'POST',
            data: { provider, fullPath, host, username, password },
            xhrFields: isTextFile ? {} : { responseType: 'blob' },
            beforeSend: function () {
                btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
            },
            success: function (data) {
                $('#file-list-container').hide();
                $('#file-viewer-container').show();

                if (isTextFile) {
                    $('#pdf-image-viewer').hide().empty();
                    $('#viewer-content').next('.CodeMirror').show();
                    initializeEditor();
                    let mode = "xml";
                    if (fullPath.toLowerCase().endsWith('.json')) mode = { name: "javascript", json: true };
                    editor.setOption("mode", mode);
                    editor.setValue(data);
                    currentXmlContent = fullPath.toLowerCase().endsWith('.xml') ? data : '';
                    editor.refresh();
                } else {
                    $('#viewer-content').next('.CodeMirror').hide();
                    $('#viewer-content').hide(); 

                    $('#pdf-image-viewer').show().empty();
                    const blob = new Blob([data]);
                    const url = window.URL.createObjectURL(blob);

                    if (fullPath.toLowerCase().endsWith('.pdf')) {
                        $('#pdf-image-viewer').html(`<iframe src="${url}" style="width:100%; height:100%; border:0;"></iframe>`);
                    } else {
                        $('#pdf-image-viewer').html(`<img src="${url}" style="max-width:100%; max-height:100%; object-fit: contain;" />`);
                    }
                }
            },
            error: function (err) {
                alert('Error viewing file: ' + err.responseText);
            },
            complete: function () {
                btn.prop('disabled', false).text('View');
            }
        });
    });

    // Handles clicking the "Back to Files" button in the file viewer.
    $('#back-to-files-btn').on('click', function () {
        $('#file-viewer-container').hide();
        $('#file-list-container').show();
        $('#open-file-details').hide(); 
        if (editor) {
            editor.setValue('');
        }
    });



    // Shows the "Add Virtual File" modal.
    $('#add-file-btn').on('click', function () {
        $('#addFileModal').modal('show');
    });

    // Handles saving a new virtual file path from the modal.
    $('#saveVirtualFile').on('click', function () {
        const saveButton = $(this);
        const filePath = $('#modal-filePath').val();

        if (!filePath) {
            alert('Please enter a file path.');
            return;
        }

        saveButton.prop('disabled', true).html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Saving...');

        $.ajax({
            url: '/Explorer/AddVirtualFile',
            type: 'POST',
            data: { filePath: filePath },
            success: function (response) {
                $('#addFileModal').modal('hide');
                $('#addFileForm')[0].reset();
                alert('Success! The page will now reload to show the new file.');
                location.reload();
            },
            error: function (xhr) {
                alert('Error: ' + xhr.responseText);
            },
            complete: function () {
                saveButton.prop('disabled', false).text('Save');
            }
        });
    });

    $('#copy-document').on('click', function () {
        if (editor && editor.getValue()) {
            navigator.clipboard.writeText(editor.getValue()).then(() => alert('Document copied!'));
        }
    });

    $('#copy-tag').on('click', function () {
        if (editor && editor.getSelection()) {
            navigator.clipboard.writeText(editor.getSelection()).then(() => alert('Selection copied!'));
        } else {
            alert('No text selected in the editor.');
        }
    });

    $('#copy-address').on('click', function () {
        navigator.clipboard.writeText(currentFileAddress).then(() => alert('Address copied!'));
    });

    $('#copy-path').on('click', function () {
        if (currentXmlContent) {
            const selectedText = editor.getSelection();
            $.post('/Explorer/GetXPathFromSelection', { xmlContent: currentXmlContent, selectedText: selectedText }, function (xpath) {
                navigator.clipboard.writeText(xpath).then(() => {
                    if (selectedText) {
                        alert('XPath for your selection has been copied!');
                    } else {
                        alert('All XPaths for the entire document have been copied!');
                    }
                });
            }).fail(function (err) {
                alert("Error getting XPath: " + err.responseText);
            });
        } else {
            alert('This function is only available for XML files.');
        }
    });

    $(document).on('click', '.clickable-row', function (e) {
        if ($(e.target).closest('.view-file-btn').length) {
            return;
        }
        $(this).find('.view-file-btn').click();
    });
});